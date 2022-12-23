# GetMoarFediverse

This is a small app that shows how you could use [FakeRelay](https://github.com/g3rv4/FakeRelay/) to import content into your instance that's tagged with hashtags you're interested in.

This doesn't paginate over the tags, that means it will import up to 20 statuses per instance. This also keeps a txt file with all the statuses it has imported.

## Features

GetMoarFediverse can download either from a list of predefined tags or it can connect to you Mastodon's PostgreSQL database and pull the hashtags your users are following.

The `FakeRelayApiKey` on the `config.json` is optional. If you don't provide one, you need to pass you FakeRelay key as an environment variable named `FAKERELAY_APIKEY`.

### Pulling from a predefined list of tags

This `config.json` pulls two tags from two instances:

```
{
    "FakeRelayUrl": "https://fakerelay.gervas.io",
    "FakeRelayApiKey": "1TxL6m1Esx6tnv4EPxscvAmdQN7qSn0nKeyoM7LD8b9mz+GNfrKaHiWgiT3QcNMUA+dWLyWD8qyl1MuKJ+4uHA==",
    "Tags": [ "dotnet", "csharp" ],
    "Instances": [ "hachyderm.io", "mastodon.social" ]
}
```

### Downloading all the followed hashtags of your instance

You can pass `MastodonPostgresConnectionString` with a connection string to your postgres database and GetMoarFediverse will download content for all the hashtags the users on your server follow. Here's an example:

```
{
    "FakeRelayUrl": "https://fakerelay.gervas.io",
    "FakeRelayApiKey": "1TxL6m1Esx6tnv4EPxscvAmdQN7qSn0nKeyoM7LD8b9m+GNfrKaHiWgiT3QcNMUA+dWLyWD8qyl1MuKJ+4uHA==",
    "MastodonPostgresConnectionString": "Host=myserver;Username=mastodon_read;Password=password;Database=mastodon_production",
    "Instances": [ "hachyderm.io", "mastodon.social" ]
}
```

## How can I run it?

There are many ways for you to run GetMoarFediverse:

* [You can run it as a GitHub Action](#running-it-as-a-github-action). Thank you [@chdorner](https://github.com/chdorner)! this is awesome!
  * This means you don't have to have anything running on your infrastructure!
  * This also means you need to maintain the list of tags you want to pull (they can't be updated dynamically, as GitHub Actions workers can't connect to your postgres database (hopefully!))
* [You can run a prebuilt executable](#download-a-prebuilt-executable)
  * You need to run it in your infrastructure
  * You can download content with the tags your users are following
  * This is the fastest way to run it
  * Executables are trimmed so that they're smaller than 10 MB
* [You can run it as a docker container](#you-can-run-it-on-docker)
  * You need to run it in your infrastructure
  * You can download content with the tags your users are following
  * This gives you better isolation, whatever GetMoarFediverse does, it can't escape its container

### Running it as a GitHub Action

I recommend you watch [this demo I recorded](https://youtu.be/XOBD8OsdjGY). Basically you can run GetMoarFediverse as a GitHub Action, so that you don't have to think about setting anything up on your infrastructure:

1. Fork [GMFActionDemo](https://github.com/g3rv4/GMFActionDemo)
2. Change the `config.json` so that you pull the tags you want
3. Add a `FAKERELAY_APIKEY` GitHub Action secret with your api key
4. Uncomment the two lines on `GetMoarFediverse.yml` that run it on a scheduled route

This was built and graciously shared back to the main repo by [@chdorner](https://github.com/chdorner). Thanks a ton!

### Download a prebuilt executable

You can download an executable for your environment [on the releases page](https://github.com/g3rv4/GetMoarFediverse/releases). In addition to that, you will need to create a `config.json` file with your desired logic. Once you have that file, all you need to run is `./GetMoarFediverse /path/to/config.json`. You can put it on a cron like this :)

```
1,16,31,46 * * * * /path/to/GetMoarFediverse /path/to/config.json > /path/to/GetMoarFediverse/cron.log 2>&1
```

You will find an executable for Windows as well, which you can use on a scheduled task.

### You can run it on docker

You can use docker compose for this. This `docker-compose.yml` shows how it can be used:

```
version: '2'
services:
  importdata:
    image: 'ghcr.io/g3rv4/getmoarfediverse:latest'
    volumes:
      - '/path/to/GetMoarFediverse/data:/data'
```

On `/path/to/GetMoarFediverse/data`, you need to place a `config.json` that tells the system what you want.

Once you have that set up, you can just execute it! and it will output what's going on.

```
g3rv4@s1:~/docker/FakeRelay$ docker-compose run --rm importdata
Fetching tag #dotnet from mastodon.social
Fetching tag #hachyderm from hachyderm.io
Fetching tag #dotnet from hachyderm.io
Fetching tag #csharp from mastodon.social
Fetching tag #csharp from hachyderm.io
Bringing in https://dotnet.social/users/mzikmund/statuses/109458968117245196
```

You can run it as a cron every 15 minutes, doing something like this:

```
1,16,31,46 * * * * /usr/local/bin/docker-compose -f /path/to/GetMoarFediverse/docker-compose.yml run --rm importdata > /path/to/GetMoarFediverse/cron.log 2>&1
```

### What about Docker on Windows?

I'd recommend you use the executable... but if you really want to, you can run it on Docker on Windows and set up a scheduled task. You can watch [this demo by Jeff Lindborg](https://www.youtube.com/watch?v=v73ZKtP0rzE).
