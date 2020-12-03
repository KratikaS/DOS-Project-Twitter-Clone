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
                wire  = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
            }
            actor.serialization-bindings {
                ""System.Object"" = wire 
            }
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote {
                maximum-payload-bytes = 30000000 bytes
                dot-netty.tcp {
                    hostname = 127.0.0.1
                    port = 9004
                    message-frame-size =  30000000b
                    send-buffer-size =  30000000b
                    receive-buffer-size =  30000000b
                    maximum-frame-size = 30000000b
                    tcp-reuse-addr = off
                }
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
               
            |TweetMsg(actorRef,tweetMsg, i,printAct)->
                numTweets<-numTweets+1
                totalOperations<-totalOperations-1
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
                for j in hTag do
                    if(hashTagMap.ContainsKey(j)=false)then
                        hashTagMap.Add(j,tempHashTagMap)
                    hashTagMap.Item(j).Add(tweetMsg)|>ignore
                let mentions=tweetMsg.Mentions
                for j in mentions do
                    if(mentionsMap.ContainsKey(j)=false)then
                        mentionsMap.Add(j,tempMentionMap)
                    mentionsMap.Item(j).Add(tweetMsg)|>ignore
                //TweetDictionary.Add(actorRef,tweetMsg)
                let mutable str1 = "live feed from user "
                str1<- str1 + (actorRef.Path.Name)
                let mutable str2 = " to "
                str1 <- str1+str2
                let str3 = ","
                let str4 = " received"
                for j in Subscribers.Item(actorRef) do
                    if(RegisteredAccounts.Item(j)=true)then
                        str1 <- str1+  (j.Path.Name)+str3
                        //j<!LiveFeed(tweetMsg,actorRef)
                       // printAct <! PrintLive(i,j.Path.Name|>int)
                str1<-str1+str4
                printAct <! PrintLive(str1)
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
                    let mutable tempRandom=NodeRandom.Next(0,SubscriberList.Count)
                    while(SubscriberList.Item(tempRandom)=mailbox.Sender()) do
                          tempRandom<-NodeRandom.Next(0,SubscriberList.Count)
                    Subscribers.Item(mailbox.Sender()).Add(SubscriberList.Item(tempRandom)) |>ignore
                    if((SubscribedTo.ContainsKey(SubscriberList.Item(tempRandom)))=false)then
                        SubscribedTo.Add(SubscriberList.Item(tempRandom),tempSt)
                    SubscribedTo.Item(SubscriberList.Item(tempRandom)).Add(mailbox.Sender()) |>ignore
                //printfn "subscribe to a specific client"
            |QuerySubs(user,printAct)->
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
                printAct<!PrintQuerySubs(tweetList)
                if(totalOperations=0)then
                    mailbox.Context.Self<!GetSubscriberRanksInfo
            |QueryTag(tag, user,printAct)->
                numQueries<-numQueries+1
                totalOperations<-totalOperations-1
                if (tag<>" ") then
                    let tweetList=new List<Tweet>()
                    if(hashTagMap.ContainsKey(tag)<>false)then
                        for i in hashTagMap.Item(tag) do
                            tweetList.Add(i)                 
                    printfn "Hashtag queries by user %i %s" user tag
                    printAct<!PrintQueryTag(tweetList,tag)
                if(totalOperations=0)then
                    mailbox.Context.Self<!GetSubscriberRanksInfo
                //mailbox.Sender()<!PrintTweets(tweetList) 
            |QueryMentions(actorRef, queryingActor, queriedActor,printAct)->
                numQueries<-numQueries+1
                totalOperations<-totalOperations-1
                let tweetList=new List<Tweet>()
                if(mentionsMap.ContainsKey(actorRef)<>false)then
                    for i in mentionsMap.Item(actorRef) do
                        tweetList.Add(i)
                //mailbox.Sender()<!PrintTweets(tweetList) 
                
                printf "user %i" queryingActor
                printfn " mentioned the following actor in its query %A" queriedActor
                printAct<!PrintQueryMention(tweetList,actorRef.Path.Name)
                if(totalOperations=0)then
                    mailbox.Context.Self<!GetSubscriberRanksInfo
            |Retweet(retweeter, tweetBy, actorRef,printAct)->
                 numTweets<-numTweets+1
                 totalOperations<-totalOperations-1
                 //mailbox.Sender()<!PrintRetweet(user,TweetDictionary)
                 
                 if(tweetMsgMap.ContainsKey(actorRef)) then
                    printf "user %i"retweeter
                    printfn " retweeted "
                    let NodeRandom = new Random()
                    let tweet=tweetMsgMap.Item(actorRef).SelectVariation(1,NodeRandom).First()
                    printAct<! PrintRetweet(tweet,retweeter,tweetBy)
                    
                 if(totalOperations=0)then
                     mailbox.Context.Self<!GetSubscriberRanksInfo
                    //printfn " retweeted the tweet %A" (retweetList.Item(actorList.Item(menActorNum)))
            |Logout->
                totalOperations<-totalOperations-1
                if(RegisteredAccounts.Item(mailbox.Sender())=true)then
                    RegisteredAccounts.Item(mailbox.Sender())<-false
                    printfn "User %s logged out" (mailbox.Sender().Path.Name)
                if(totalOperations=0)then
                    mailbox.Context.Self<!GetSubscriberRanksInfo
            |Login->
                totalOperations<-totalOperations-1
                if(RegisteredAccounts.Item(mailbox.Sender())=false)then
                    RegisteredAccounts.Item(mailbox.Sender())<-true
                    printfn "User %s Logged In" (mailbox.Sender().Path.Name)
                if(totalOperations=0)then
                    mailbox.Context.Self<!GetSubscriberRanksInfo
            |GetSubscriberRanksInfo->
                for i in Subscribers do
                    printfn "%i" (i.Value.Count)
                timer1.Stop()
                printfn "elapsed %d ms" timer1.ElapsedMilliseconds
                printfn "Total Tweets processed %i" numTweets
                printfn "Total Queries processed %i" numQueries
                let timeNum=timer1.ElapsedMilliseconds|>double
                printfn "Time per operation %f" (double finalOperations/timeNum) 
            |_ -> printf ""
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