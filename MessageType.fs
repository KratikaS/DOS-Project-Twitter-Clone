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
    |Sample of String
    |Register of IActorRef
    |TweetMsg of IActorRef * Tweet
    |Subscribe of IActorRef
    |QuerySubs
    |QueryTag of String
    |QueryMentions of IActorRef
    |Logout
    |Done
    |PrintTweets of List<Tweet>
    |SubscriptionDone of IActorRef
    |Simulate

