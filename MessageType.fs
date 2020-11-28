module MessageType

open System
open Akka.Actor
open Akka.FSharp

type Tweet=
    |TweetMsg of String
    |HashTag of List<String>
    |Mentions of List<IActorRef>

type TweeterEngine =
    |Sample of String
    |Register of IActorRef
    |TweetMsg of IActorRef * Tweet
    |Subscribe of IActorRef
    |QuerySubs
    |QueryTag of String
    |QueryMentions of IActorRef
    |Logout

