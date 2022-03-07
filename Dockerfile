FROM ubuntu:20.04

RUN apt-get update
RUN apt-get install -y wget

RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN rm packages-microsoft-prod.deb

RUN apt-get update
RUN apt-get install -y apt-transport-https dotnet-sdk-6.0

WORKDIR /app
COPY . /app

ENTRYPOINT [ "/app/InspectCode.sh" ]