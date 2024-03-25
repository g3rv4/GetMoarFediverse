# GetMoarFediverse

This is a small app that shows how you could use [FakeRelay](https://github.com/g3rv4/FakeRelay/) to import content into your instance that's tagged with hashtags you're interested in.

This doesn't paginate over the tags, that means it will import up to 20 statuses per instance. This also keeps a txt file with all the statuses it has imported.

## Features

GetMoarFediverse can download either from a list of predefined tags or it can connect to you Mastodon's PostgreSQL database and pull the hashtags your users are following.

The `FakeRelayApiKey` on the `config.json` is optional. If you don't provide one, you need to pass you FakeRelay key as an environment variable named `FAKERELAY_APIKEY`.

### Pulling from a predefined list of tags

This `config.json` pulls two tags from two instances:

```json
{
    "FakeRelayUrl": "https://fakerelay.gervas.io",
    "FakeRelayApiKey": "1TxL6m1Esx6tnv4EPxscvAmdQN7qSn0nKeyoM7LD8b9mz+GNfrKaHiWgiT3QcNMUA+dWLyWD8qyl1MuKJ+4uHA==",
    "Tags": [ "dotnet", "csharp" ],
    "Instances": [ "hachyderm.io", "mastodon.social" ]
}
```

### Downloading all the followed hashtags of your instance using the database

You can pass `MastodonPostgresConnectionString` with a connection string to your postgres database and GetMoarFediverse will download content for all the hashtags the users on your server follow. Here's an example:

```json
{
    "FakeRelayUrl": "https://fakerelay.gervas.io",
    "FakeRelayApiKey": "1TxL6m1Esx6tnv4EPxscvAmdQN7qSn0nKeyoM7LD8b9m+GNfrKaHiWgiT3QcNMUA+dWLyWD8qyl1MuKJ+4uHA==",
    "MastodonPostgresConnectionString": "Host=myserver;Username=mastodon_read;Password=password;Database=mastodon_production",
    "Instances": [ "hachyderm.io", "mastodon.social" ]
}
```

#### And download all the hashtags pinned by your users!

If you add `"PinnedTags": true`, you can also populate the hashtags pinned by your users :) thanks [@nberlee](https://github.com/nberlee), this is great!

```json
{
    "FakeRelayUrl": "https://fakerelay.gervas.io",
    "FakeRelayApiKey": "1TxL6m1Esx6tnv4EPxscvAmdQN7qSn0nKeyoM7LD8b9m+GNfrKaHiWgiT3QcNMUA+dWLyWD8qyl1MuKJ+4uHA==",
    "MastodonPostgresConnectionString": "Host=myserver;Username=mastodon_read;Password=password;Database=mastodon_production",
    "PinnedTags": true,
    "Instances": [ "hachyderm.io", "mastodon.social" ]
}
```

### Downloading the hashtags followed by users via the API

You can pass an `Api` object and GetMoarFediverse will download content for all the hashtags for each user for whom an access token is provided. Here's an example:

```json
{
  "FakeRelayUrl": "https://foo.example",
  "FakeRelayApiKey": "blah==",
  "Api": {
    "Url": "https://mastodon.example/api/",
    "Tokens": [
      {
        "Owner": "Chris",
        "Token": "1413D6izFoQdu0x00000DZ9ufcBvhOt7hoxuctHg2c"
      }
    ]
  },
  "Instances": [ "hachyderm.io", "mastodon.social" ]
}
```

For the `Tokens` array items, both `Owner` and `Token` are required fields. Owner can be any non-empty string that would identify the owner of the token (e.g. could be the Mastodon username, app client ID, etc). This data structure allows multiple user accounts to be supported.

To create an access token for the config file, visit the web interface of your Mastodon instance and go to `/settings/applications` (Settings > Development). The token only requires `read:follows` scope. Then copy the access token shown at the top of the screen.

> If a database connection string is also provided via `MastodonPostgresConnectionString`, the tags will be retrieved via the database and any API-related settings will be ignored.

## How can I run it?

There are many ways for you to run GetMoarFediverse:

* [You can run it as a GitHub Action](#running-it-as-a-github-action). Thank you [@chdorner](https://github.com/chdorner), this is awesome!
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
4. Uncomment the two lines on `GetMoarFediverse.yml` that run it on a scheduled route. Keep in mind that [the schedule event can be delayed during periods of high loads of GitHub Actions workflow runs](https://docs.github.com/en/actions/using-workflows/events-that-trigger-workflows#schedule).

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