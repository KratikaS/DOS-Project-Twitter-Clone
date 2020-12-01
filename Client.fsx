#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: MathNet.Numerics"
#load "MessageType.fs"
//#load "Server.fsx"
//open Server
open MathNet.Numerics
open MessageType
//open Server
open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.Remote
open System.Collections.Generic
open System.Diagnostics

let totalActors = 100
let mutable Counter=0
let mutable operationCount=0
let actorList=new List<IActorRef>();
let mutable operations =new List<string>();
operations.Add("Tweet");
operations.Add("QueryTags")
operations.Add("QuerySubs")
operations.Add("QueryMentions")
//operations.Add("Subscribe")
let mutable HashList=new List<string>()
let mutable distribution=new Dictionary<int,int>()
//operators<-{"Tweet";"QueryTags";"QuerySubs";"QueryMentions"}
let ranStr n : string = 
    let r = new System.Random()
    new System.String(Array.init n (fun _ -> char (r.Next(97,123))))
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
        }"

//let system = System.create "RemoteActorFactory" config
let system = ActorSystem.Create("ActorFactory", config) 
//let system = System.create "system" (Configuration.defaultConfig())
let serv=system.ActorSelection("akka.tcp://RemoteFSharp@127.0.0.1:9001/user/Server")
let Client(mailbox:Actor<_>)=
    
    let rec loop() = actor {
        let! msg=mailbox.Receive()
        match msg with
            |Sample(s)->
                printfn "Sample message"
            |TweetMsg(actorRef,tweetMsg)->
                //printfn "TweetMessage"
                serv<!TweetMsg(actorRef,tweetMsg)
            |Subscribe(num)->
                serv<!Subscribe(num)
                //printfn "subscribe to a specific client"
            |QuerySubs->
                serv<!QuerySubs
                //printfn "Query subscribers"
            |QueryTag(tag)->
                serv<!QueryTag(tag)
                //printfn "Query tags"
            |QueryMentions(actorRef)->
                serv<!QueryMentions(actorRef)
                //printfn "Query Mentions"
            |Logout->
                printfn "logout"
            |PrintTweets(tweetList)->
                //printfn "print tweets"
                printfn "%A" tweetList

        return! loop()
    }
    loop()







let Simulator(mailbox:Actor<_>)=
    let rec loop()=actor{
        let! msg = mailbox.Receive()
        match msg with
            |Register(dummy)->
                //printfn "kvdsvnjdv"
                //let mutable arr : int array = Array.zeroCreate totalActors
                //Distributions.Zipf.Samples(arr,0.3,totalActors)
                //for i in arr do
                //    printfn "%i" i
                //printfn "registering"
                let actorRef=spawn system (string Counter) Client
                serv<!Register(actorRef)
            |Simulate->
                printfn "start simulation"
                while(operationCount<1) do
                    operationCount<-operationCount+1
                    printfn "operations %i" operationCount
                    let newRandom = new Random()
                    let mutable opNum=newRandom.Next(0,operations.Count)
                    //let mutable opNum=0
                    //printfn "%s" (operations.Item(opNum))
                    match operations.Item(opNum) with  
                        |"Tweet"->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
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
                                    mentionsSet.Add(actorList.Item(menRan))
                            let twt={tweetText=tweetTxt;HashTag=hashTagList;Mentions=mentionsSet}
                            actorList.Item(actorNum)<!TweetMsg(actorList.Item(actorNum),twt)
                        |"QueryTags"->
                            if(HashList.Count>0)then
                                let mutable actorNum=newRandom.Next(0,actorList.Count)
                                let mutable tagNum=newRandom.Next(0,HashList.Count)
                                actorList.Item(actorNum)<!QueryTag(HashList.Item(tagNum))
                        |"QuerySubs"->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            actorList.Item(actorNum)<!QuerySubs
                        |"QueryMentions"->

                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            let newRandom2 = new Random()
                            let mutable menActorNum=newRandom2.Next(0,actorList.Count)
                            while menActorNum=actorNum do
                                printfn "this while loop2 %i %i" menActorNum actorNum
                                menActorNum<-newRandom2.Next(0,actorList.Count)
                            actorList.Item(actorNum)<!QueryMentions(actorList.Item(menActorNum))
                        //|"Subscribe"->
                        //    let mutable arr : int array = Array.zeroCreate totalActors
                        //    Distributions.Zipf.Samples(arr,1.1,totalActors)
                        //    for i in arr do
                        //        if(distribution.ContainsKey(i)=false)then
                        //            distribution.Add(i,0)
                        //        distribution.Item(i)<-distribution.Item(i)+1  
                        //    for i in distribution do
                        //        actorList.Item(i.Key)<!Subscribe(i.Value)
                        //    //let mutable actorNum=newRandom.Next(0,actorList.Count)
                        //    //let mutable subsRan = Distributions.Zipf.Sample(2.0,actorList.Count-1)
                        //    //actorList.Item(actorNum)<!Subscribe(actorList.Item(subsRan))
                serv<!GetSubscriberRanksInfo
   
            |SubscriptionDone(actorRef)->
                Counter<-Counter+1
                printfn "Counter=%i" Counter
                actorList.Add(actorRef)
                if(Counter=totalActors) then
                    //serv<!GetSubscriberRanksInfo
                    mailbox.Context.Self<!Subscribe(0)
                    mailbox.Context.Self<!Simulate
                else
                    mailbox.Context.Self<!Register(actorRef)
            |Subscribe(num)->
                let mutable arr : int array = Array.zeroCreate totalActors
                Distributions.Zipf.Samples(arr,1.5,totalActors)
                
                for i in arr do
                    //printfn "sample value is %i" i
                    if(distribution.ContainsKey(i)=false)then
                        distribution.Add(i,0)
                    distribution.Item(i)<-distribution.Item(i)+1  
                for i in distribution do
                    actorList.Item(i.Key)<!Subscribe(i.Value)
        return! loop()
    }
    loop()
let sim = spawn system "Sim" Simulator
sim<!Register(sim)
//let serv=spawn system "Server" Server.Server


//let res = Async.RunSynchronously serv
//printfn "this is servererrrerere%A" serv
//serv<!Sample("hello")

    
    //Server.server<!Register(actorRef)

////let Tweet twtMsg={tweetText:"hello";HashTag:["#1","#2"];Mentions:{actorList.Item(5)}}
//let twt={tweetText="hello";HashTag=["#1";"#2"];Mentions=[actorList.Item(5)]}
//serv<!TweetMsg(actorList.Item(0),twt)
//Async.RunSynchronously <| Async.Sleep(30000)

Console.ReadKey()|>ignore
    
