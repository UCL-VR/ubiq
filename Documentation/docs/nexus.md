# Nexus

## Overview

The UCL VECG hosts multiple instances of the rendezvous server on nexus.cs.ucl.ac.uk. Different branches of this repository are checked on nexus and run on different ports.

The checkouts are in `/home/node` and follow the format `ubiq-[branch name]`.

Currently `ubiq-master` is running on `8004`. This is the primary, public server.

It is expected and encouraged that feature branches are created, run on nexus temporarily for development, then removed when no longer needed.

The following sections describe how Nexus is maintained by the VECG team. You do not need to follow this pattern to maintain your own server, but it may be instructive.

## Administration

Access to `nexus.cs.ucl.ac.uk` is via SSH.

The nodejs process is managed via [pm2](https://pm2.keymetrics.io/). The relevant commands are:

* `pm2 list` (Shows running processes)
* `pm2 log` (Shows the logs)
* `pm2 flush` (Clears the logs)
* `pm2 restart` (Restarts the app, e.g. after an update)

pm2 is responsible for restarting the nodejs process after a server restart. To save the state of the tasks that it will try to restore, give the command `pm2 save`.

nodejs and pm2 run under the `node` account. All maintenance should be performed as `node`. The username/password is node/node. It is not possible to SSH directly as node; login with your CS credentials, then change user with `su` (i.e. `$ su node`).

All VECG members who request access will be given sudo permission. All members will be collaborators on the Github repository. Any member can add new members.


### Git

The node account has been given access to the GitHub repository through a [Deploy Key](https://docs.github.com/en/developers/overview/managing-deploy-keys). This is a single-use SSH key associated with the repository. 

#### New

When cloning a new copy for a branch, you must specify the folder as git will always clone into the repository name by default. For example:
`git clone --depth 1 git@github.com:UCL-VR/ubiq.git ubiq-master`

You can specify the branch name for the clone command (`git clone --depth 1 --branch master git@github.com:UCL-VR/ubiq.git ubiq-master`), or checkout the appropriate branch after.

The `--depth 1` command downloads only the `HEAD`, which is all that is needed to run the server.

After cloning, navigate to `~/ubiq-[branch name]/Node/` and issue the commands:

1. `npm install`
2. `pm2 start app.js --name "ubiq-[branch name]"`

The first installs the nodejs dependencies and the second creates the pm2 job with a unique name to identify the instance.

#### Updating

To update a checkout, enter the repository and issue `git pull`. `node` cannot write to the repository.

## Firewall

Be aware when creating your own clones that you will need to open your chosen ports on the local firewall and reload it.

To see the firewall rules on CentOS, give the command,

```
 sudo iptables -L -n -v --line-n
```

The `-n` argument shows the port numbers, rather than showing the names of typical services that run on them. `-v` shows the interface, and `--line-n` shows line numbers, which will be important when adding rules.

To add a new rule give the commands,

```
sudo iptables -I INPUT 5 -p tcp --dport 8004 -j ACCEPT
sudo service iptables save
```

The number after `INPUT` indicates the line that the rule should be added at. The `ACCEPT` rule must come before the catch-all `REJECT` rule. For example, the `REJECT` rule is on line 11 below and will reject all packets not matching any of the rules above it.

```
1    ACCEPT     all  --  0.0.0.0/0            0.0.0.0/0            state RELATED,ESTABLISHED
2    ACCEPT     icmp --  0.0.0.0/0            0.0.0.0/0
3    ACCEPT     all  --  0.0.0.0/0            0.0.0.0/0
4    ACCEPT     tcp  --  0.0.0.0/0            0.0.0.0/0            state NEW tcp dpt:22
5    ACCEPT     tcp  --  0.0.0.0/0            0.0.0.0/0            tcp dpt:8006
6    ACCEPT     tcp  --  0.0.0.0/0            0.0.0.0/0            tcp dpt:8005
7    ACCEPT     tcp  --  0.0.0.0/0            0.0.0.0/0            tcp dpt:8004
8    ACCEPT     tcp  --  0.0.0.0/0            0.0.0.0/0            tcp dpt:8003
9    ACCEPT     tcp  --  0.0.0.0/0            0.0.0.0/0            tcp dpt:8002
10   ACCEPT     tcp  --  0.0.0.0/0            0.0.0.0/0            tcp dpt:8001
11   REJECT     all  --  0.0.0.0/0            0.0.0.0/0            reject-with icmp-host-prohibited
```

The `iptables save` is for the `iptables-service`, which is installed on `Nexus`, and allows saving and automatically reloading firewall rules on reboot.

Be aware also that opening the ports locally will not make them available on the public internet, but only via the CS VPN. Nexus has range `8000-8020` open on the CS and ISD firewalls.

## Legacy Versions

When breaking changes are made, legacy versions of the server will be checked out with their last supported version number, e.g. `ubiq-0.0.6`, and instances of these will be left running. The ports that they listen on will be incremented with each breaking change.

Old clients will therefore continue to work, though old and new versions cannot communicate with eachother.

Not all versions include breaking changes, so the sequence of legacy versions running is not continuous. The latest version is always `ubiq-master`.

Currently `ubiq-0.0.6` is running on `8001`.


## References
* https://upcloud.com/community/tutorials/configure-iptables-centos/