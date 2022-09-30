// Prerequisites
// npm install @aws-sdk/client-ec2
// npm install ssh2

const { EC2Client, DescribeInstancesCommand } = require("@aws-sdk/client-ec2");
const { Client } = require('ssh2');
const fs = require("fs");

// Add the EC2 credentials here

const client = new EC2Client({
    region: "eu-west-2",
    credentials:{
        accessKeyId:"AKIAZ27FF4J4WXMJB74Q",
        secretAccessKey: "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"
    }
});

// The private key of the pair used to create the instance (or instance template).
// Convert this to a PEM if necessary using putty-gen.
// VMs will need the ssh-rsa key type added (https://github.com/steelbrain/node-ssh/issues/438) until ssh2 fixes this bug: https://github.com/mscdex/ssh2/issues/989

const instancePrivateKey = fs.readFileSync("./aws-vm-privatekey.pem");

// This function uses the EC2Client to list all the VMs in the accounts region. 
// The Tags property is used to filter out any that are used for the server.

async function getClientInstances(){
    const command = new DescribeInstancesCommand({});
    var data = await client.send(command);
    var instances = [];
    for(var reservation in data.Reservations){
        for(var instance in data.Reservations[reservation].Instances){
            var vm = data.Reservations[reservation].Instances[instance];
            if(vm.State.Code != 16){
                continue;
            }
            var tags = vm.Tags.map(x=>x.Key);
            if(tags.includes("ubiq-client")){ // Identify the VMs by the existence of a tag ("ubiq-client")
                instances.push(vm);
            }            
        }
    }
    return instances;
}

async function startInstances()
{
    var instances = await getClientInstances();
   
    for(var instance of instances){  // Copy the ubiq files with SCP.
        try{

            // This is the SSH2 client (an implementation of an SSH client in Js)
            let client = new Client(); 
 
            await client.connect({
                host: instance.PublicDnsName,
                port: 22,
                username: "ubuntu", // The username "ubuntu" is set by the AMI. This is the username set by the typical Ubuntu instance.
                privateKey: instancePrivateKey,
            })

            var build = "HexGrid";

            client.on("ready",()=>{
                client.shell((err,stream)=>{
                    stream.on("close", ()=>{ console.log("Closed") });
                    //stream.on("data", (data)=>console.log(data.toString())); // Uncomment this line to get the output of the session 
                    stream.on("exit", ()=>{ client.end() });
                    stream.on("error", console.log);

                    stream.write(`./${build}/ubiq.x86_64 -batchmode -nographics &\n`);
                    stream.write(`./${build}/ubiq.x86_64 -batchmode -nographics &\n`);
                    stream.write(`./${build}/ubiq.x86_64 -batchmode -nographics &\n`);
                    stream.write(`./${build}/ubiq.x86_64 -batchmode -nographics &\n`);
                    stream.write(`./${build}/ubiq.x86_64 -batchmode -nographics &\n`);

                    // When this process exits, so will the above processes
                });
            });
        
            console.log(`Started five instances on ${instance.InstanceId} ${instance.PublicDnsName}`);

        }catch(e){
            console.log(e);
        }
    }
}

startInstances();
