#time
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: MathNet.Numerics"
#load "MessageType.fs"
open MathNet.Numerics
open MessageType
open System
open Akka.Actor
open Akka.FSharp
open Akka.Remote
open System.Collections.Generic
open Akka.Serialization
open System.Linq
open System.Diagnostics
let config =  
    Configuration.parse
        @"akka {
            actor.serializers{
                json  = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                bytes = ""Akka.Serialization.ByteArraySerializer""
            }
            actor.serialization-bindings {
                ""System.Byte[]"" = bytes
                ""System.Object"" = json 
                
            }
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            
            remote.helios.tcp {
            hostname = 127.0.0.1
            port = 9004
            send-buffer-size = 5120000b
            receive-buffer-size = 5120000b
            maximum-frame-size = 1024000b
            tcp-keepalive = on
            }
            
        }"



let mutable totalOperations=0
let mutable finalOperations=0;
let mutable maxSubs=0;
let mutable numQueries=0;
let mutable numTweets=0;


let system = System.create "RemoteFSharp" config
let timer1 = new Stopwatch()
//let system = System.create "system" (Configuration.defaultConfig())
let sampleActor(mailbox:Actor<_>)=
    let rec loop()=actor{
        let! msg=mailbox.Receive();
        printfn "%A" msg
        return! loop()
    }
    loop()
let sample=spawne system "sample" <@ sampleActor @>[]
let Server(mailbox:Actor<_>)=
    let mutable SubscriberList=new List<IActorRef>()
    let mutable SubscribedTo=new Dictionary<IActorRef,HashSet<IActorRef>>()
    let mutable Subscribers=new Dictionary<IActorRef,HashSet<IActorRef>>()
    let mutable TweetDictionary=new Dictionary<IActorRef,Tweet>()
    let mutable RegisteredAccounts=new Dictionary<IActorRef,bool>()
    let mutable tweetMsgMap = new Dictionary<IActorRef,HashSet<Tweet>>()
    let mutable mentionsMap = new Dictionary<IActorRef,HashSet<Tweet>>()
    let mutable hashTagMap = new Dictionary<String,HashSet<Tweet>>()
    //let mutable allTweet = new List<TweetMsg>()
    let rec loop() = actor {
        let! msg=mailbox.Receive()
        match msg with
            |InitializeValues(num)->
                totalOperations<-num
                finalOperations<-num
            |StartTimers->
                timer1.Start()
            |Sample(s)->
                printfn "Sample Message"
                mailbox.Sender()<!"Done"
            |Register(actorRef,userNumber)->
                printfn "Registered user number %i" userNumber
                RegisteredAccounts.Add(actorRef,true)
                
                SubscriberList.Add(actorRef)
                mailbox.Sender()<!SubscriptionDone(actorRef)
               
            |TweetMsg(actorRef,tweetMsg, i)->
                numTweets<-numTweets+1
                totalOperations<-totalOperations-1
                printfn "operations remaining %i" totalOperations
                if(RegisteredAccounts.Item(actorRef)=false)then
                    RegisteredAccounts.Item(actorRef)<-true
                    printfn "User %s logged in" (mailbox.Sender().Path.Name)
                let tempMsgMap=new HashSet<Tweet>()
                let tempHashTagMap = new HashSet<Tweet>()
                let tempMentionMap = new HashSet<Tweet>()

                if(tweetMsgMap.ContainsKey(actorRef)=false) then
                    tweetMsgMap.Add(actorRef,tempMsgMap)
                
                tweetMsgMap.Item(actorRef).Add(tweetMsg)|>ignore
                let hTag=tweetMsg.HashTag
                for i in hTag do
                    if(hashTagMap.ContainsKey(i)=false)then
                        hashTagMap.Add(i,tempHashTagMap)
                    hashTagMap.Item(i).Add(tweetMsg)|>ignore
                let mentions=tweetMsg.Mentions
                for i in mentions do
                    if(mentionsMap.ContainsKey(i)=false)then
                        mentionsMap.Add(i,tempMentionMap)
                    mentionsMap.Item(i).Add(tweetMsg)|>ignore
                //TweetDictionary.Add(actorRef,tweetMsg)
                for i in Subscribers.Item(actorRef) do
                    if(RegisteredAccounts.Item(i)=true)then
                        i<!LiveFeed(tweetMsg,actorRef)
                printfn "Tweet done by user number %A" i
                if(totalOperations=0)then
                    mailbox.Context.Self<!GetSubscriberRanksInfo
            |Subscribe(num)->
                let NodeRandom = new Random()
                let tempSt=new HashSet<IActorRef>()
                let tempS=new HashSet<IActorRef>()
                if(Subscribers.ContainsKey(mailbox.Sender())=false)then
                    Subscribers.Add(mailbox.Sender(),tempS)
                while(Subscribers.Item(mailbox.Sender()).Count <=num)do
                    let tempRandom=NodeRandom.Next(0,SubscriberList.Count)
                    Subscribers.Item(mailbox.Sender()).Add(SubscriberList.Item(tempRandom))
                    if(SubscribedTo.ContainsKey(SubscriberList.Item(tempRandom))=false)then
                        SubscribedTo.Add(SubscriberList.Item(tempRandom),tempSt)
                    SubscribedTo.Item(SubscriberList.Item(tempRandom)).Add(mailbox.Sender())
                //printfn "subscribe to a specific client"
            |QuerySubs(user)->
                numQueries<-numQueries+1
                totalOperations<-totalOperations-1
                printfn "operations remaining %i" totalOperations
                let tweetList=new List<Tweet>()
                if(SubscribedTo.ContainsKey(mailbox.Sender())=true)then
                    let followingList=SubscribedTo.Item(mailbox.Sender())
                    for i in followingList do
                        if(tweetMsgMap.ContainsKey(i)<>false)then
                            for j in tweetMsgMap.Item(i) do
                                tweetList.Add(j)
                    //mailbox.Sender()<!PrintTweets(tweetList)    
                printfn "subscribers queried by user %i" user 
                if(totalOperations=0)then
                    mailbox.Context.Self<!GetSubscriberRanksInfo
            |QueryTag(tag, user)->
                numQueries<-numQueries+1
                totalOperations<-totalOperations-1
                printfn "operations remaining %i" totalOperations
                let tweetList=new List<Tweet>()
                if(hashTagMap.ContainsKey(tag)<>false)then
                    for i in hashTagMap.Item(tag) do
                        tweetList.Add(i)
                
                printfn "Hashtag queries by user %i %s" user tag
                if(totalOperations=0)then
                    mailbox.Context.Self<!GetSubscriberRanksInfo
                //mailbox.Sender()<!PrintTweets(tweetList) 
            |QueryMentions(actorRef, queryingActor, queriedActor)->
                numQueries<-numQueries+1
                totalOperations<-totalOperations-1
                printfn "operations remaining %i" totalOperations
                let tweetList=new List<Tweet>()
                if(mentionsMap.ContainsKey(actorRef)<>false)then
                    for i in mentionsMap.Item(actorRef) do
                        tweetList.Add(i)
                //mailbox.Sender()<!PrintTweets(tweetList) 
                
                printf "user %i" queryingActor
                printfn " mentioned the following actor in its query %A" queriedActor
                if(totalOperations=0)then
                    mailbox.Context.Self<!GetSubscriberRanksInfo
            |Retweet(retweeter, tweetBy, actorRef)->
                 numTweets<-numTweets+1
                 totalOperations<-totalOperations-1
                 printfn "operations remaining %i" totalOperations
                 //mailbox.Sender()<!PrintRetweet(user,TweetDictionary)
                 
                 if(tweetMsgMap.ContainsKey(actorRef)) then
                    printf "user %i"retweeter
                    printfn " retweeted "
                    let NodeRandom = new Random()
                    let tweet=tweetMsgMap.Item(actorRef).SelectVariation(1,NodeRandom).First()
                    mailbox.Sender()<! PrintRetweet(tweet,retweeter,tweetBy)
                 if(totalOperations=0)then
                     mailbox.Context.Self<!GetSubscriberRanksInfo
                    //printfn " retweeted the tweet %A" (retweetList.Item(actorList.Item(menActorNum)))
            |Logout->
                totalOperations<-totalOperations-1
                printfn "operations remaining %i" totalOperations
                if(RegisteredAccounts.Item(mailbox.Sender())=true)then
                    RegisteredAccounts.Item(mailbox.Sender())<-false
                    printfn "User %s logged out" (mailbox.Sender().Path.Name)
                if(totalOperations=0)then
                    mailbox.Context.Self<!GetSubscriberRanksInfo
            |Login->
                totalOperations<-totalOperations-1
                printfn "operations remaining %i" totalOperations
                if(RegisteredAccounts.Item(mailbox.Sender())=false)then
                    RegisteredAccounts.Item(mailbox.Sender())<-true
                    printfn "User %s Logged In" (mailbox.Sender().Path.Name)
                if(totalOperations=0)then
                    mailbox.Context.Self<!GetSubscriberRanksInfo
            |GetSubscriberRanksInfo->
                printfn "user   Subs"
                printfn "size of Subscribers %i" Subscribers.Count
                for i in Subscribers do
                    printfn "%i" (i.Value.Count)
                timer1.Stop()
                printfn "elapsed %d ms" timer1.ElapsedMilliseconds
                printfn "Total Tweets processed %i" numTweets
                printfn "Total Queries processed %i" numQueries
                let timeNum=timer1.ElapsedMilliseconds|>double
                printfn "Time per operation %f" (double finalOperations/timeNum) 

        return! loop()
    }
    loop()

let server = spawne system "Server" <@ Server @>[]
//let serv=system.ActorSelection("akka://system/user/Server")
//server<!Sample("hello")

printfn "%A" server

//printfn "Waiting on port 9001"
// while true do
Console.ReadKey() |> ignore
0