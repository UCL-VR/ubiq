FROM ubuntu:22.04

# This snippet installs:
#	curl, which is required to install Node Version Manager, ensuring we can get the correct version of Node.
#	git, in order to clone Ubiq
#	pwgen, which is required to generate secrets local to this container
#	dos2unix, a utility used to make sure we don't have any windows line endings in place, which will mess with bash.
#	coturn, a TURN server

RUN apt-get -y update
RUN apt-get -y install curl git pwgen dos2unix coturn

# This snippet (from Steve Campbell @ https://stackoverflow.com/questions/25899912/how-to-install-nvm-in-docker) installs NVM and the specified version of Node.

ENV NVM_DIR /root/.nvm
RUN mkdir -p $NVM_DIR
RUN curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.7/install.sh | bash
ENV NODE_VERSION v18.12.1
RUN /bin/bash -c "source $NVM_DIR/nvm.sh && nvm install $NODE_VERSION && nvm use --delete-prefix $NODE_VERSION"

ENV NODE_PATH $NVM_DIR/versions/node/$NODE_VERSION/lib/node_modules
ENV PATH      $NVM_DIR/versions/node/$NODE_VERSION/bin:$PATH

# The next two lines checkout the server code only. The sparse checkout clones only the metadata, to avoid copying the full history. sparse-checkout add then prompts git to download just the Node folder.

RUN git clone --filter=blob:none --sparse --branch "unity-v1.0.0-pre.3" https://github.com/UCL-VR/ubiq.git ubiq
WORKDIR ubiq
RUN git sparse-checkout add Node
WORKDIR Node
RUN npm i
WORKDIR /

# This line copies the local config template into the container. This template will be updated at runtime before the servers are started.

COPY local.json /ubiq/Node/config/local.json

# This line copies the local coturn config over the existing one. This template will be updated at runtime before the servers are started.

COPY turnserver.conf /etc/turnserver.conf

# This line copies the script that will configure and start the server at runtime. dos2unix ensures that it conforms to unix line endings.

COPY ubiq.sh /
RUN dos2unix /ubiq.sh

# This line sets the above script as the entrypoint. When this script terminates, so will the image.

ENTRYPOINT ["bash", "-c", "source /ubiq.sh"]

# Finally open the relevant ports

EXPOSE 8009-8030