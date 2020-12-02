#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: MathNet.Numerics"
#load "MessageType.fs"
open System.Diagnostics
open MathNet.Numerics
open MessageType
open System
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
            json  = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
            bytes = ""Akka.Serialization.ByteArraySerializer""
        }
        actor.serialization-bindings {
            ""System.Byte[]"" = bytes
            ""System.Object"" = json
            
        }
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
        }"
//take user input
printfn "Enter the number of operations you wish to perform"
let totalOperations=100
printfn "Enter the total number of users"
let totalActors = 100
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
let Client(mailbox:Actor<_>)=
    
    let rec loop() = actor {
        let! msg=mailbox.Receive()
        match msg with
            |Sample(s)->
                printfn "Sample message"
            |TweetMsg(actorRef,tweetMsg, i)->
                //printfn "TweetMessage"
                serv<!TweetMsg(actorRef,tweetMsg, i)
            |Subscribe(num)->
                serv<!Subscribe(num)
                //printfn "subscribe to a specific client"
            |QuerySubs(user)->
                serv<!QuerySubs(user)
                //printfn "Query subscribers"
            |QueryTag(tag,user)->
                serv<!QueryTag(tag,user)
                //printfn "Query tags"
            |QueryMentions(actorRef,queryingActor,queriedActor)->
                serv<!QueryMentions(actorRef,queryingActor,queriedActor)
                //printfn "Query Mentions"
            |Retweet(retweeter, tweetBy, actorRef)->
                serv<!Retweet(retweeter, tweetBy,actorRef)
            |Logout->
                serv<!Logout
            |Login->
                serv<!Login
            |PrintTweets(tweetList)->
                printfn "print tweets"
                printfn "%A" tweetList
            |PrintRetweet(reTweet, tweeter, user)->
                printf "user %i"tweeter 
                printfn " retweeted tweet by user %i"user
            |GetSubscriberRanksInfo->
                serv<!GetSubscriberRanksInfo
            |LiveFeed(tweet,actorRef)->
                printfn "Live Feed from %s received" (actorRef.Path.Name)


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
                printfn "start simulation"
                let newRandom = new Random()
                while(operationCount<=totalOperations) do
                    operationCount<-operationCount+1
                    ///printfn "operations %i" operationCount
                    
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
                            actorList.Item(actorNum)<!TweetMsg(actorList.Item(actorNum),twt, actorNum)
                        |"QueryTags"->
                            if(HashList.Count>0)then
                                let mutable actorNum=newRandom.Next(0,actorList.Count)
                                let mutable tagNum=newRandom.Next(0,HashList.Count)
                                printf "user number %i" actorNum
                                printfn " quereing hash tag %s" (HashList.Item(tagNum))
                                actorList.Item(actorNum)<!QueryTag(HashList.Item(tagNum),actorNum)
                        |"QuerySubs"->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            actorList.Item(actorNum)<!QuerySubs(actorNum)
                        |"QueryMentions"->

                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            let newRandom2 = new Random()
                            let mutable menActorNum=newRandom2.Next(0,actorList.Count)
                            while menActorNum=actorNum do
                                printfn "this while loop2 %i %i" menActorNum actorNum
                                menActorNum<-newRandom2.Next(0,actorList.Count)
                                
                            actorList.Item(actorNum)<!QueryMentions(actorList.Item(menActorNum),actorNum, menActorNum)
                         |"Retweet" -> 
                             let mutable actorNum=newRandom.Next(0,actorList.Count)
                             let mutable tweetBy=newRandom.Next(0,actorList.Count)
                             while(actorNum = tweetBy) do 
                                    tweetBy<-newRandom.Next(0,actorList.Count)
                             actorList.Item(actorNum)<!Retweet(actorNum,tweetBy,actorList.Item(tweetBy)) 
                         |"Zipfian retweet"->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            let mutable tweetBy=zipfianUsers.Item(newRandom.Next(0,zipfianUsers.Count))
                            while(actorNum = tweetBy) do 
                                tweetBy<-newRandom.Next(0,actorList.Count)
                            actorList.Item(actorNum)<!Retweet(actorNum,tweetBy,actorList.Item(tweetBy))
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
                                    mentionsSet.Add(actorList.Item(menRan))
                            let twt={tweetText=tweetTxt;HashTag=hashTagList;Mentions=mentionsSet}
                            actorList.Item(actorNum)<!TweetMsg(actorList.Item(actorNum),twt, actorNum)
                         |"Login" ->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            actorList.Item(actorNum)<!Login
                         |"Logout" ->
                            let mutable actorNum=newRandom.Next(0,actorList.Count)
                            actorList.Item(actorNum)<!Logout
                
                
                
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
            
        return! loop()
    }
    loop()
let sim = spawn system "Sim" Simulator
printfn "Initializing the users and its subscribers according to zipf distribution"
serv<!InitializeValues(totalOperations)
sim<!Register(sim,0)
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