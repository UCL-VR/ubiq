The loads involved in the scalability tests require the Bot clients to be spread over a number of machines.

# AWS

To conduct the tests we use AWS.

The experiment branch includes scripts (Javascript, run by NodeJs) that shepard the AWS instances. There are scripts for copying builds to the Bot instances, and starting the bot processes.


## Templates and Tags

In our AWS account, we set up a template, which includes the key-pair used for SSH authetnication, through which all managment is completed.

Note that SSH currently has a bug which reports the key type as rsa-ssh, which has been deprecated by OpenSSH.

As a workaround, the template should use the User Data field to execute a command to add ssh-rsa to the accepted key types.

https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/user-data.html

#!/bin/bash
sed -i '$aPubkeyAcceptedKeyTypes=+ssh-rsa' /etc/ssh/sshd_config
service sshd restart
sudo apt-get install unzip

The snippet adds the line to the end of the ssh service config, and restarts it. It also installs unzip to make copying the builds faster.

Resource Tags are used to identify which machines are used for the server, and which for clients.

Since the same template is used for the server, the ubiq-client tag should be set each time Launch from Template is used.

For the scalability tests, 10 t2.medium intances were used. Each instance ran five processes, with each process running two bots.

For completeness, we commit the private key and leave the hardcoded credentials. These are not attached to any running VMs, and should be replaced when a new set group is constructed.


## Node Scripts

### File Uploads

ssh-sftp is used to upload builds automatically to each ubiq-client tagged instance. The builds to copy are specified in the source. To speed up copying, ZIPs of the builds are copied and extracted using the ssh2 exec method.

It is assumed the builds are zipped manually.


### Bot Processes

Bot processes are started over ssh. Even though multiple processes are started in the background, they are all tied to the ssh session. when the local Node program terminates, so does the ssh session, and with it the Bot processes.
