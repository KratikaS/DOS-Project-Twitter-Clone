module MessageType

open System
open Akka.Actor
open Akka.FSharp
open System.Collections.Generic

type Tweet=
    {
        tweetText : String;
        HashTag : List<String>;
        Mentions : HashSet<IActorRef>;
    }

type TweeterEngine =
    |InitializeValues of int
    |StartTimers
    |Sample of String
    |Register of IActorRef * int
    |TweetMsg of IActorRef * Tweet * int
    |Subscribe of int
    |QuerySubs of int
    |QueryTag of String * int
    |QueryMentions of IActorRef * int * int
    |Login
    |Logout
    |PrintTweets of List<Tweet>
    |SubscriptionDone of IActorRef
    |Simulate
    |GetSubscriberRanksInfo
    |Retweet of int * int *IActorRef
    |PrintRetweet of Tweet * int * int
    |LiveFeed of Tweet * IActorRef