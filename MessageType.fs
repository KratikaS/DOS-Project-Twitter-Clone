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
    |InitializeValues of int *IActorRef
    |StartTimers
    |Sample of String
    |Register of IActorRef * int
    |TweetMsg of IActorRef * Tweet * int * IActorRef
    |Subscribe of int
    |QuerySubs of int *IActorRef
    |QueryTag of String * int *IActorRef
    |QueryMentions of IActorRef * int * int *IActorRef
    |Login
    |Logout
    |SubscriptionDone of IActorRef
    |Simulate
    |GetSubscriberRanksInfo
    |Retweet of int * int *IActorRef * IActorRef
    |PrintRetweet of Tweet * int * int
    |LiveFeed of Tweet * IActorRef
    |PrintLive of string 
    |PrintQueryTag of string
    |PrintQueryMention of  string
    |PrintQuerySubs 
    |Done