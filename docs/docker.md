# Running GetMoarFediverse on Docker

You can use docker compose for this. This `docker-compose.yml` shows how it can be used:

```
version: '2'
services:
  importdata:
    image: 'ghcr.io/g3rv4/getmoarfediverse:latest'
    volumes:
      - '/path/to/GetMoarFediverse/data:/data'
```

On `/path/to/GetMoarFediverse/data`, you need to place a `config.json` that tells the system what you want. You could use something like this:

```
{
    "FakeRelayUrl": "https://fakerelay.gervas.io",
    "FakeRelayApiKey": "1TxL6m1Esx6tnv4EPxscvAmdQN7qSn0nKeyoM7LD8b9mz+GNfrKaHiWgiT3QcNMUA+dWLyWD8qyl1MuKJ+4uHA==",
    "Tags": [
        "dotnet",
        "csharp"
    ],
    "Sites": [
        {
            "Host": "hachyderm.io",
            "SiteSpecificTags": [
                "hachyderm"
            ]
        },
        {
            "Host": "mastodon.social"
        }
    ]
}
```

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

I'm running it as a cron every 15 minutes, doing something like this:

```
1,16,31,46 * * * * /usr/local/bin/docker-compose -f /path/to/GetMoarFediverse/docker-compose.yml run --rm importdata > /path/to/GetMoarFediverse/cron.log 2>&1
```

### What about Windows?

You can run it on Docker on Windows, and set up a scheduled task. You can watch [this demo by Jeff Lindborg](https://www.youtube.com/watch?v=v73ZKtP0rzE).

I could package it as an installer, and that would remove the need of Docker... would you be interested in that? Open an issue :)
