#!/bin/bash

echo "Starting Ubiq Server Container"

export UBIQ_TURNSECRET=$(pwgen 20 1)

# This snippet generates the self-signed certificate that will be used if one is not provided via the mount option
# The -node option ensures the key does not require a keyphrase
# This must be executed after the hostname is known
# If testing this command on windows, consider prepending it with winpty as sometimes any interactive prompt(s) can get messed up.

if [ -d /certs ]; then
	echo "Using host certificates."
else
	echo "Generating certificates for $HOSTNAME"
	mkdir /certs
	cd /certs
	openssl req -new -nodes -x509 -subj "/CN=$HOSTNAME/emailAddress=ubiq@$HOSTNAME/C=UK/ST=London/L=Gower Street/O=UCL/OU=Computer Science" -keyout key.pem -out cert.pem > /dev/null 2>&1
fi


# This next section configures the deployment. Configurations are provided in files, so these lines replace placeholders with environment variables before starting the services.

sed -i -e "s/UBIQ_TURNSECRET/$UBIQ_TURNSECRET/g" /ubiq/Node/config/local.json
sed -i -e "s/UBIQ_TURNSECRET/$UBIQ_TURNSECRET/g" /etc/turnserver.conf

sed -i -e "s/UBIQ_HOSTNAME/$HOSTNAME/g" /ubiq/Node/config/local.json
sed -i -e "s/UBIQ_HOSTNAME/$HOSTNAME/g" /etc/turnserver.conf

# Start coturn (in the background)

coturn &
sleep 1

cd /ubiq/Node

npm start

# This last line is used for creating an interactive prompt. Uncomment it for diagnostics and debugging.

# /bin/bash "$@"

