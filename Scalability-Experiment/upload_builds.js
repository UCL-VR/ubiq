// This script copies the Unity builds to the AWS VM's assigned to run the clients.
// It identifies the client VMs based on their tags.
// To use this script, set the builds array with the subdirectories containing
// the Unity builds. All executables should be called 'ubiq', with different builds
// being placed in different directories.

// Prerequisites
// npm install @aws-sdk/client-ec2
// npm install node-scp

const { EC2Client, DescribeInstancesCommand } = require("@aws-sdk/client-ec2");
const Client = require('ssh2-sftp-client');
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
// VMs will need the ssh-rsa key type added (https://github.com/steelbrain/node-ssh/issues/438) until ssh2 fixes this bug (https://github.com/mscdex/ssh2/issues/989)
// This is done when the template is instantiated if the User Data is set properly (see Readme for more instructions)

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

// The sftp client is a wrapper around an ssh2 client, which we can also use to execute commands.
// This function wraps the exec command in a Promise allowing it to be used with await.

async function ssh_exec(sftpclient, command){
    return new Promise((resolve,reject)=>{
        sftpclient.client.exec(command, // The inner client is the general ssh2 client
            (err,stream)=>{
                if(err) throw err;
                stream.on("data", (data, extended)=>{
                    // console.log(data.toString()); // uncomment to get debug information
                });
                stream.on("exit", (code, signal)=>{
                    if(code != 0){
                        console.log(code);
                    }
                    resolve(); 
                });
                stream.on("error", console.log);
            });
        }
    );
}

async function uploadToInstance(instance)
{
    try{

        // This is the SSH2 client (an implementation of an SSH client in Js)
        let client = new Client(); 

        await client.connect({
            host: instance.PublicDnsName,
            port: 22,
            username: "ubuntu", // The username "ubuntu" is set by the AMI. This is the username set by the typical Ubuntu instance.
            privateKey: instancePrivateKey,
        })

        // By convention, all executables are called 'ubiq', and the directory distinguishes the build.
        // All builds are compressed into a zip file (no subdirectories inside the zip), also called ubiq.
        // The contents of the zip are extracted into a remote subdirectory with the Build name

        var builds = ["HexGrid"];

        for(var build of builds) {

            console.log(`Uploading ${build} to ${instance.InstanceId}...`);

            await client.mkdir(`/home/ubuntu/${build}/`);

            await client.fastPut(
                `D:/UCL/Ubiq Master/Unity/Build/Linux/${build}/ubiq.zip`,
                `/home/ubuntu/${build}/ubiq.zip`
            );

            await ssh_exec(client, `unzip -o /home/ubuntu/${build}/ubiq.zip -d /home/ubuntu/${build}/`);
            await ssh_exec(client, `sudo chmod 775 /home/ubuntu/${build}/ubiq.x86_64`);
            
            await client.end();

            console.log(`Uploaded to ${instance.InstanceId} ${instance.PublicDnsName}`);
                     
        };

    }catch(e){
        console.log(e);
    }
}

async function uploadFiles()
{
    var instances = await getClientInstances();
    await Promise.all(instances.map(instance => uploadToInstance(instance)));
}

uploadFiles();
