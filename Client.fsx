#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: MathNet.Numerics"
#load "MessageType.fs"
open System.Diagnostics
open MathNet.Numerics
open MessageType
open System
open System.IO
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.Remote
open System.Collections.Generic
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
                    message-frame-size =  30000000b
                    send-buffer-size =  30000000b
                    receive-buffer-size =  30000000b
                    maximum-frame-size = 30000000b
                }
            }
        }"
//take user input
let args : string array = fsi.CommandLineArgs |> Array.tail
let totalOperations = args.[0] |> int
let totalActors = args.[1] |>int
let timer2 = new Stopwatch()
//let totalOperations=1000
//printfn "Enter the total number of users"
//let totalActors = 1000
let mutable Counter=0
let mutable operationCount=0
let actorList=new List<IActorRef>();
let mutable operations =new List<string>();
operations.Add("Tweet");
operations.Add("QueryTags")
operations.Add("QuerySubs")
operations.Add("QueryMentions")
operations.Add("Retweet")
operations.Add("Login")
operations.Add("Logout")
operations.Add("Zipfian tweet")
operations.Add("Zipfian retweet")
//operations.Add("Subscribe")
let mutable HashList=new List<string>()
let mutable distribution=new Dictionary<int,int>()
let mutable zipfianUsers = new List<int>()
//operators<-{"Tweet";"QueryTags";"QuerySubs";"QueryMentions"}
let ranStr n : string = 
    let r = new System.Random()
    new System.String(Array.init n (fun _ -> char (r.Next(97,123))))

let system = ActorSystem.Create("ActorFactory", config) 
//let system = System.create "system" (Configuration.defaultConfig())
let serv=system.ActorSelection("akka.tcp://RemoteFSharp@127.0.0.1:9004/user/Server")
let path = "./clientlogs.txt"
let PrinterActor(mailbox:Actor<_>)=
    let rec loop() = actor {
        let! msg=mailbox.Receive()
        match msg with
           |PrintLive(str)->
              //let path = "./clientlogs.txt"
                printfn "%s" str
           |PrintRetweet(reTweet, tweeter, user)->
                printf "user %i"tweeter 
                printfn " retweeted tweet by user %i"user
           |PrintQuerySubs->
                printfn "Query response received for subscribers query"
           |PrintQueryTag(tag)->
               printfn "Query response received for %s query" tag
           |PrintQueryMention(user)->
               printfn "Query response received for mentioned user %s query" user
           |Done->
               timer2.Stop()
               printfn "time taken to complete %i operations: %d ms" totalOperations timer2.ElapsedMilliseconds
               printfn "Please press any key to end the simulation"
           |_-> printf ""
        return! loop()
    }
    loop()
let printAct = spawn system "PrintAct" PrinterActor

let Client(mailbox:Actor<_>)=
    
    let rec loop() = actor {
        let! msg=mailbox.Receive()
        match msg with
            |Sample(s)->
                printfn "Sample message"
            |TweetMsg(actorRef,tweetMsg, i,printAct)->
                //printfn "TweetMessage"
                serv<!TweetMsg(actorRef,tweetMsg, i,printAct)
            |Subscribe(num)->
                serv<!Subscribe(num)
                //printfn "subscribe to a specific client"
            |QuerySubs(user,printAct)->
                serv<!QuerySubs(user,printAct)
                //printfn "Query subscribers"
            |QueryTag(tag,user,printAct)->
                serv<!QueryTag(tag,user,printAct)
                //printfn "Query tags"
            |QueryMentions(actorRef,queryingActor,queriedActor,printAct)->
                serv<!QueryMentions(actorRef,queryingActor,queriedActor,printAct)
                //printfn "Query Mentions"
            |Retweet(retweeter, tweetBy, actorRef,printAct)->
                serv<!Retweet(retweeter, tweetBy,actorRef,printAct)
            |Logout->
                serv<!Logout
            |Login->
                serv<!Login
            
                
           
            |GetSubscriberRanksInfo->
                serv<!GetSubscriberRanksInfo
            |LiveFeed(tweet,actorRef)->
                printfn "Live Feed from %s received" (actorRef.Path.Name)
            |_-> printf ""

        return! loop()
    }
    loop()
let mutable actorCounter  = -1
let Simulator(mailbox:Actor<_>)=
    let rec loop()=actor{
        let! msg = mailbox.Receive()
        match msg with
            |Register(dummy,0)->
                let actorRef=spawn system (string Counter) Client
                actorCounter <- actorCounter+1
                serv<!Register(actorRef, actorCounter)
            |Simulate->
                timer2.Start()
                printfn "start simulation"
                let newRandom = new Random()
                while(operationCount<=totalOperations) do
                    operationCount<-operationCount+1
                    
                    let mutable opNum=newRandom.Next(0,operations.Count)
                    //let mutable opNum=0
                    //printfn "%s" (operations.Item(opNum))
                    match operations.Item(opNum) with  
                        |"Tweet"->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            let tweetTxt=(ranStr (newRandom.Next(1,10)))
                            let mutable hashTagList= new List<string>()
                            let mutable mentionsSet=new HashSet<IActorRef>()
                            let hashTagNum=newRandom.Next(0,5)
                            let maxMentions = min (actorList.Count-1) 5
                            let mentionsNum=newRandom.Next(0,maxMentions)
                            for i =0 to hashTagNum do
                                let hashtag="#"+(ranStr (newRandom.Next(1,5)))
                                hashTagList.Add(hashtag)
                                HashList.Add(hashtag)
                            
                            while mentionsSet.Count < mentionsNum do
                                //printfn "this while loop1 %i" actorList.Count
                                let mutable menRan=newRandom.Next(0,actorList.Count)
                                if(mentionsSet.Contains(actorList.Item(menRan))=false && actorList.Item(menRan)<>actorList.Item(actorNum)) then
                                    mentionsSet.Add(actorList.Item(menRan)) |>ignore
                            let twt={tweetText=tweetTxt;HashTag=hashTagList;Mentions=mentionsSet}
                            actorList.Item(actorNum)<!TweetMsg(actorList.Item(actorNum),twt, actorNum,printAct)
                        |"QueryTags"->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            if(HashList.Count>0)then
                                let mutable tagNum=newRandom.Next(0,HashList.Count)
                                printf "user number %i" actorNum
                                printfn " quereing hash tag %s" (HashList.Item(tagNum))
                                actorList.Item(actorNum)<!QueryTag(HashList.Item(tagNum),actorNum,printAct)
                            else
                                actorList.Item(actorNum)<!QueryTag(" ",actorNum,printAct)
                                
                        |"QuerySubs"->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            actorList.Item(actorNum)<!QuerySubs(actorNum,printAct)
                        |"QueryMentions"->

                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            let newRandom2 = new Random()
                            let mutable menActorNum=newRandom2.Next(0,actorList.Count)
                            while menActorNum=actorNum do
                                //printfn "this while loop2 %i %i" menActorNum actorNum
                                menActorNum<-newRandom2.Next(0,actorList.Count)
                                
                            actorList.Item(actorNum)<!QueryMentions(actorList.Item(menActorNum),actorNum, menActorNum,printAct)
                         |"Retweet" -> 
                             let mutable actorNum=newRandom.Next(0,actorList.Count)
                             let mutable tweetBy=newRandom.Next(0,actorList.Count)
                             while(actorNum = tweetBy) do 
                                    tweetBy<-newRandom.Next(0,actorList.Count)
                             actorList.Item(actorNum)<!Retweet(actorNum,tweetBy,actorList.Item(tweetBy),printAct) 
                         |"Zipfian retweet"->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            let mutable tweetBy=zipfianUsers.Item(newRandom.Next(0,zipfianUsers.Count))
                            while(actorNum = tweetBy) do 
                                tweetBy<-newRandom.Next(0,actorList.Count)
                            actorList.Item(actorNum)<!Retweet(actorNum,tweetBy,actorList.Item(tweetBy),printAct)
                         |"Zipfian tweet"->
                            let mutable actorNum=zipfianUsers.Item(newRandom.Next(0,zipfianUsers.Count))
                            let tweetTxt=(ranStr (newRandom.Next(1,100)))
                            let mutable hashTagList= new List<string>()
                            let mutable mentionsSet=new HashSet<IActorRef>()
                            let hashTagNum=newRandom.Next(0,20)
                            let mentionsNum=newRandom.Next(0,actorList.Count-1)
                            for i =0 to hashTagNum do
                                let hashtag="#"+(ranStr (newRandom.Next(1,10)))
                                hashTagList.Add(hashtag)
                                HashList.Add(hashtag)
                            
                            while mentionsSet.Count < mentionsNum do
                                //printfn "this while loop1 %i" actorList.Count
                                let mutable menRan=newRandom.Next(0,actorList.Count)
                                if(mentionsSet.Contains(actorList.Item(menRan))=false && actorList.Item(menRan)<>actorList.Item(actorNum)) then
                                    mentionsSet.Add(actorList.Item(menRan)) |>ignore
                            let twt={tweetText=tweetTxt;HashTag=hashTagList;Mentions=mentionsSet}
                            actorList.Item(actorNum)<!TweetMsg(actorList.Item(actorNum),twt, actorNum,printAct)
                         |"Login" ->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            actorList.Item(actorNum)<!Login
                         |"Logout" ->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            actorList.Item(actorNum)<!Logout
                         |_ -> printf ""
                
                
            |SubscriptionDone(actorRef)->
                Counter<-Counter+1
                //printfn "Counter=%i" Counter
                actorList.Add(actorRef)
                if(Counter=totalActors) then
                    //serv<!GetSubscriberRanksInfo
                    mailbox.Context.Self<!Subscribe(0)
                    mailbox.Context.Self<!StartTimers
                    mailbox.Context.Self<!Simulate
                    //mailbox.Context.Self<!StartTimers
                    //serv<!StartTimers
                else
                    mailbox.Context.Self<!Register(actorRef,0)
            |Subscribe(num)->
                let mutable arr : int array = Array.zeroCreate totalActors
                Distributions.Zipf.Samples(arr,1.5,totalActors)
                
                for i in arr do
                    //printfn "sample value is %i" i
                    if(distribution.ContainsKey(i)=false)then
                        distribution.Add(i,0)
                    distribution.Item(i)<-distribution.Item(i)+1  
                for i=0 to (totalActors-1) do
                    if(distribution.ContainsKey(i)=true)then
                        let zipfUserFreq=distribution.Item(i)
                        zipfianUsers.Add(i)
                        actorList.Item(i)<!Subscribe(zipfUserFreq)
                    else
                        actorList.Item(i)<!Subscribe(1)
            |StartTimers->
                serv<!StartTimers
            
            |_ -> printf ""
            
        return! loop()
    }
    loop()


let sim = spawn system "Sim" Simulator
printfn "Initializing the users and its subscribers according to zipf distribution"
serv<!InitializeValues(totalOperations,printAct)
sim<!Register(sim,0)

Console.ReadKey()|>ignore