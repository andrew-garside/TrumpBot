﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Meebey.SmartIrc4net;
using SharpRaven;
using SharpRaven.Data;
using TrumpBot.Configs;
using TrumpBot.Models;
using Tweetinvi;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Logic;
using Tweetinvi.Models;
using Tweetinvi.Streaming;
using Tweetinvi.Streaming.Parameters;
using User = Tweetinvi.User;

namespace TrumpBot.Modules
{
    internal class TwitterStream
    {
        private readonly IrcClient _ircClient;
        private readonly Thread _thread;
        private readonly IrcBot _ircBot;
        private TwitterStreamConfigModel.StreamConfig _config;
        private IAuthenticatedUser _authenticatedUser;
        internal IFilteredStream FilteredStream;
        private ILog _log = LogManager.GetLogger(typeof(TwitterStream));
        private string _breadcrumbName = "TwitterStream Thread";
        private RavenClient _ravenClient = Services.Raven.GetRavenClient();

        internal TwitterStream(IrcClient client)
        {
            _ircClient = client;
            
            LoadConfig();

            _authenticatedUser = Services.Twitter.GetTwitterUser();
            
            _ravenClient?.AddTrail(
                new Breadcrumb(_breadcrumbName) {Message = "Authenticating to Twitter", Level = BreadcrumbLevel.Info});
            
            if (_authenticatedUser == null)
            {
                var exception = ExceptionHandler.GetLastException();
                _log.Debug(
                    $"When attempting to authenticate with Twitter, got exception {exception.TwitterDescription}");
                _log.Debug("Self destructing the Tweet thread");
                _ravenClient?.Capture(new SentryEvent(exception.WebException));
                return;
            }

            _thread = new Thread(() => TweetThread());
            _thread.Start();
        }

        internal void LoadConfig()
        {
            _config = (TwitterStreamConfigModel.StreamConfig) new TwitterStreamConfig().LoadConfig();
            _log.Debug("TwitterStream has loaded its config");
        }

        private void TweetThread()
        {
            _ravenClient?.AddTrail(new Breadcrumb(_breadcrumbName)
            {
                Message = $"{_breadcrumbName} started",
                Level = BreadcrumbLevel.Info
            });

            _log.Debug("Tweet thread started!");

            if (!_config.Enabled)
            {
                return;
            }

            FilteredStream = Stream.CreateFilteredStream();
            FilteredStream.FilterLevel = StreamFilterLevel.None;
            FilteredStream.StallWarnings = true;

            foreach (var stream in _config.Streams)
            {
                FilteredStream.AddFollow(stream.TwitterUserId);
                _log.Debug($"Added {stream.TwitterUserId} to tracking");
            }

            _ravenClient?.AddTrail(new Breadcrumb(_breadcrumbName)
            {
                Message = "Added configured IDs to stream",
                Level = BreadcrumbLevel.Info
            });

            FilteredStream.MatchingTweetReceived += (sender, args) =>
            {
                ITweet tweet = args.Tweet;
                _log.Debug($"Found tweet from {tweet.CreatedBy.ScreenName}");

                TwitterStreamConfigModel.Stream stream =
                    _config.Streams.Find(s => s.TwitterUserId == tweet.CreatedBy.Id);

                if (stream == null)
                {
                    return;
                }
                Dictionary<string, string> data =
                    new Dictionary<string, string>
                    {
                        {"Twitter ID", tweet.CreatedBy.IdStr},
                        {"Tweet ID", tweet.IdStr},
                        {"Tweet Body", tweet.FullText},
                        {"Destination channels", string.Join(", ", stream.Channels)}
                    };
                _ravenClient?.AddTrail(new Breadcrumb(_breadcrumbName)
                {
                    Message = $"Found matching Tweet, from {tweet.CreatedBy.Name}",
                    Level = BreadcrumbLevel.Info,
                    Data = data
                });

                if (stream.IgnoreRetweets && tweet.IsRetweet)
                {
                    _log.Debug(
                        $"Ignoring tweet {tweet.IdStr} as IgnoreRetweets is {stream.IgnoreRetweets} and IsRetweet is {tweet.IsRetweet}");
                    return;
                }

                if (stream.IgnoreReplies && tweet.InReplyToUserId != null)
                {
                    _log.Debug(
                        $"Ignoring tweet {tweet.IdStr} as IgnoreReplies is {stream.IgnoreReplies} and InReplyToUserId is not null (it is {tweet.InReplyToUserId})");
                    return;
                }

                if (tweet.IsRetweet)
                {
                    if (stream.RetweetsToIgnoreByUserId.Contains(tweet.RetweetedTweet.CreatedBy.Id))
                    {
                        _log.Debug($"Ignoring tweet {tweet.IdStr} as author has been ignored");
                        return;
                    }
                }

                foreach (string channel in stream.Channels)
                {
                    if (!_ircClient.JoinedChannels.Contains(channel)) continue;
                    
                    _log.Debug($"Sending tweet from {tweet.CreatedBy.Name} to {channel}");
                    if (_ircClient.IsConnected)
                    {
                        if (tweet.IsRetweet)
                        {
                            _ircClient.SendMessage(SendType.Message, channel,
                                $"{IrcConstants.IrcBold}{tweet.CreatedBy.Name} (@{tweet.CreatedBy.ScreenName}):{IrcConstants.IrcNormal} RT @{tweet.RetweetedTweet.CreatedBy.ScreenName} {WebUtility.HtmlDecode(tweet.RetweetedTweet.FullText.ReplaceNonPrintableCharacters(' ').Replace('\n', ' ').Replace('\r', ' '))} - {tweet.Url}");
                            return;
                        }
                        _ircClient.SendMessage(SendType.Message, channel,
                            $"{IrcConstants.IrcBold}{tweet.CreatedBy.Name} (@{tweet.CreatedBy.ScreenName}):{IrcConstants.IrcNormal} {WebUtility.HtmlDecode(tweet.FullText.ReplaceNonPrintableCharacters(' ').Replace('\n', ' ').Replace('\r', ' '))} - {tweet.Url}");
                        return;
                    }
                    _log.Debug("Tried to send message to channel but IRC bot is not connected");
                }
            };

            FilteredStream.StreamStopped += (sender, args) =>
            {
                _log.Debug("Twitter stream disconnected with following exception");
                _log.Debug(args.Exception.StackTrace);
                _ravenClient?.Capture(new SentryEvent(args.Exception) {Message = "Stream stopped"});
                if (args.DisconnectMessage != null) // If socket closed for "reasons" this will be null
                {
                    _log.Debug(
                        $"Twitter disconnect message was: ({args.DisconnectMessage.Code}) {args.DisconnectMessage.Reason}");
                }
                while (true)
                {
                    _log.Debug("Attempting to reconnect to Twitter");
                    FilteredStream.StartStreamMatchingAnyCondition();
                }
            };

            FilteredStream.WarningFallingBehindDetected += (sender, args) =>
            {
                _log.Debug($"Twitter stream is falling behind. Warning from Twitter: {args.WarningMessage.Message}");
                _log.Debug($"Twitter queue is {args.WarningMessage.PercentFull}% full");
                _ravenClient?.Capture(
                    new SentryEvent($"Twitter stream falling behind, queue {args.WarningMessage.PercentFull}% full"));
            };

            FilteredStream.StartStreamMatchingAnyCondition();
        }
    }
}