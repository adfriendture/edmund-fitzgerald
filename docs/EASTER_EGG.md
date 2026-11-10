# The 1360 kc easter egg, and Gordon Lightfoot

## What's in the game now

Tune the AM receiver to **1360 kc**. A late-night DJ says someone phoned in a request for "a song about a
big ore boat on Superior," that he looked all through the stacks and can't find it, and that "maybe nobody's
written it yet." Then he plays something quiet: an original fingerpicked guitar piece the game generates on the fly.

The joke works on its own. *The Wreck of the Edmund Fitzgerald* came out in 1976, so on the night of
November 10, 1975, it really didn't exist yet.

## Why the real song isn't in the build

Putting a commercial recording in a game takes **two separate licences**:

1. **Sync licence** for the composition (the song itself). Granted by the song's music publisher.
2. **Master-use licence** for the recording. Granted by whoever owns the 1976 master, normally the record label.

A free game still needs both. "Free to play" changes the price you might negotiate, not whether you need
permission. Re-recording a cover avoids the master licence but **still needs the sync licence**, because the
composition is protected. So does a "sound-alike" that copies the melody. A guitar part that is merely in a
similar folk style is fine; that's what the game ships with.

To pursue it, find the current publisher and master owner through the performing-rights databases
(SOCAN, ASCAP or BMI repertory search), then email their licensing or sync departments. Say it's a small free
memorial game; some rights holders are generous with tributes, some won't answer.

Also worth knowing: the song is closely tied to the families of the crew. Some families have been protective of
how the wreck is portrayed, so a licence request should come with a description of the game's respectful tone.

## The hook, for if you ever get a licence

The game already supports it. Put an audio file at:

```
Assets/StreamingAssets/Radio/station_1360.ogg   (or .wav / .mp3)
```

On start-up the game loads it and plays it on 1360 instead of the generated guitar, with AM-radio filtering
and static depending on how well you're tuned. The DJ still talks over the top first.

Audio files in that folder are **gitignored** on purpose, so a licensed track can't be committed or published by
accident. Don't ship a build with the real recording in it unless you hold both licences in writing.
