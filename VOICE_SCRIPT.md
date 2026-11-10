# Edmund Fitzgerald: voice script

139 lines, grouped by character in story order, so each voice can be recorded in one sitting.

## How to record

- **Format:** WAV, mono, 44.1 or 48 kHz, 16 or 24 bit. OGG and MP3 also work.
- **File name:** exactly the line id, e.g. `and_final_01.wav`. Put the files in `Assets/StreamingAssets/Voice/`.
- **Keep it dry.** Close mic, no reverb, no EQ. The game adds the VHF crackle, the phone band-pass and the AM radio colour itself.
- Leave about a quarter second of silence before and after each take. Trim longer gaps; the game waits for the clip to finish.
- Any line without a recording still shows as a subtitle, so you can record in any order and test as you go.
- After adding files, rebuild (`tools/build_mac.sh`) or copy them into the built app's `StreamingAssets/Voice` folder.

A note on tone: these were real men. Play them as professionals doing their jobs on a bad afternoon, not as doomed characters. Nobody aboard knew how it would end, and that is what makes the last exchange land.

## You, the captain  (34 lines)

*Fifties, forty years on the Lakes. Calm, dry, never raises his voice. The player hears these as their own lines.*

| File | Line | Direction |
|---|---|---|
| `intro_capt_01` | I feel it. Hold her on one-three-zero. We'll be in the lee of Whitefish by suppertime. | Unhurried. You've been doing this for forty years. *(in the room)* |
| `capt_and_0252_01` | Go ahead, Anderson. | Holding the handset. Clipped radio manner. *(VHF radio)* |
| `capt_and_0252_02` | Rolling some. Nothing she hasn't done before. We're going to haul on down past Caribou and run for Whitefish. | Confident. *(VHF radio)* |
| `capt_dmg_01` | Anderson, this is the Fitzgerald. I have a fence rail down, two vents lost or damaged, and a list. I'm checking down. Will you stay by me till I get to Whitefish? | Measured, controlled. This is serious but you won't let it show. *(VHF radio)* |
| `capt_dmg_02` | Yes. Both of them. | Short. *(VHF radio)* |
| `capt_dmg_02b` | Starting them now. | Short. A little irritated with yourself. *(VHF radio)* |
| `capt_radar_01` | Anderson, this is the Fitzgerald. I've lost both radars. Can you keep track of me and give me some navigational help? | Calm. You need them now. *(VHF radio)* |
| `capt_nav_01` | One-four-one. Thanks, Anderson. | Brief. *(VHF radio)* |
| `capt_cg_beacon_01` | Coast Guard Sault, this is the Edmund Fitzgerald. Is the light and the radio beacon at Whitefish Point working? | Formal radio voice. *(VHF radio)* |
| `capt_cg_beacon_02` | Roger. Fitzgerald out. | Clipped. *(VHF radio)* |
| `capt_ava_01` | Any vessel near Whitefish Point, this is the Edmund Fitzgerald. Is the light at Whitefish working? | Calling blind. *(VHF radio)* |
| `capt_ava_02` | I've got a bad list. Lost both radars. And I'm taking heavy seas over the deck. One of the worst seas I've ever been in. | Tired. The truest thing you've said all day. *(VHF radio)* |
| `capt_ava_aside` | Don't allow nobody on deck. | Turning away from the handset, to someone in the pilothouse. Sharp. *(in the room)* |
| `capt_0645_01` | Roger. We'll watch for them. | Quiet. *(VHF radio)* |
| `capt_final_01` | We are holding our own. | The last words. Plain, steady, almost offhand. Do several takes. *(VHF radio)* |
| `capt_order_pumps` | Engine room, start the ballast pumps. Both of them. | Firm. *(phone)* |
| `capt_order_pumps_off` | Engine room, secure the ballast pumps. | Firm. *(phone)* |
| `capt_order_sound` | Have somebody sound the tanks and let me know. | Businesslike. *(phone)* |
| `capt_order_damage` | Deck watch. What's it look like back there? | Over the noise. *(phone)* |
| `capt_order_nodeck` | Pass the word. Nobody goes out on deck. Nobody. | Hard. *(in the room)* |
| `capt_order_engine` | Chief, how's she running? | Plain. *(phone)* |
| `capt_order_radar` | Mate, what have you got on the radar? | Over your shoulder. *(in the room)* |
| `capt_order_wheel` | Wheelsman, take her. Hold her steady on this heading. | Handing off. *(in the room)* |
| `capt_order_position` | Where are we, Mate? | Over your shoulder. *(in the room)* |
| `capt_order_lifejackets` | Pass the word. Everybody into their life jackets. | Quiet and very calm. You don't want to frighten anyone. *(in the room)* |
| `capt_order_coffee` | Galley, send some coffee up to the pilothouse. | Easy. A normal day. *(phone)* |
| `capt_and_checkin` | Anderson, Fitzgerald. How's it look back there? | Radio voice. *(VHF radio)* |
| `capt_cg_weather` | Coast Guard Sault, Fitzgerald. What's the latest on the weather? | Formal. *(VHF radio)* |
| `capt_mayday_01` | Mayday, mayday, mayday. This is the Edmund Fitzgerald, northwest of Whitefish Point. We're taking on water with a heavy list. Request assistance. | The call nobody on the real ship ever made. Steady but urgent. *(VHF radio)* |
| `capt_wheel_takes` | You have the wheel. Hold her off it. | Quick. No argument. *(in the room)* |
| `capt_offcourse_01` | Negative, Anderson. Coming back around to one-four-one. | Brief. *(VHF radio)* |
| `capt_offcourse_02` | Roger, Anderson. Coming right. | Brief. *(VHF radio)* |
| `capt_and_riding` | Riding heavy, Anderson. Taking some over the bow, but she's doing fine. | Calm. *(VHF radio)* |
| `capt_and_ack` | Roger that, Anderson. Fitzgerald out. | Brief. *(VHF radio)* |

## First Mate  (28 lines)

*In the pilothouse or the chart room just behind you. Steady, a bit younger than the captain.*

| File | Line | Direction |
|---|---|---|
| `intro_mate_01` | Wind's backed around northwest, Captain. She's starting to roll some. | From the chart room behind you. Calm, a professional. Raising his voice a little over the wind. *(in the room)* |
| `intro_mate_02` | Aye. Weather office went to storm warnings this morning. All of Lake Superior. | Matter-of-fact. *(in the room)* |
| `mate_shoal_01` | What was that? You feel that, Captain? | Startled, coming into the pilothouse. *(in the room)* |
| `mate_shoal_02` | Felt like she bumped something. We're awful close to that shoal off Caribou. | Uneasy, half to himself. *(in the room)* |
| `mate_radar_01` | Captain, I've lost the radar. Both sets. The picture's just gone. | Coming from the radar. Flat disbelief. *(in the room)* |
| `mate_radar_02` | Must have carried away the scanners up top. | Grim. *(in the room)* |
| `mate_whitefish_01` | Captain, I can't get the Whitefish beacon on the direction finder. Nothing at all. And there's no light. | Puzzled, a little scared. *(in the room)* |
| `mate_0545_01` | She's slow coming back up from these, Captain. | Quiet, close by. Watching the clinometer. *(in the room)* |
| `mate_0700_01` | Whitefish is about fifteen miles off, Captain. We'll be in the lee inside the hour. | Hope. Almost relief. *(in the room)* |
| `mate_end_01` | Captain, she's not coming up— | Cut off mid-word. Do not finish the sentence. *(in the room)* |
| `mate_nodeck` | Aye, Captain. I'll tell the galley and the watch. | Behind you. *(in the room)* |
| `mate_radar_ok` | Anderson's about ten miles astern. Caribou's off to the west. Nothing else out here but us. | At the radar. *(in the room)* |
| `mate_radar_dead` | Nothing, Captain. Both screens are dead. | At the radar. *(in the room)* |
| `mate_pos_40` | Still a long way off, Captain. Forty miles to Whitefish, give or take. | At the chart table. *(in the room)* |
| `mate_pos_30` | About thirty miles to Whitefish, Captain. | At the chart table. *(in the room)* |
| `mate_pos_20` | Twenty miles to Whitefish, Captain. Getting there. | At the chart table. *(in the room)* |
| `mate_pos_15` | Fifteen miles off Whitefish. Almost to the lee. | At the chart table. *(in the room)* |
| `mate_pos_off` | Captain, we're well off the course line. Whitefish is the other way. | At the chart table, alarmed. *(in the room)* |
| `mate_lifejackets` | ...Aye, Captain. | A long pause first. He understands what it means. *(in the room)* |
| `mate_island_01` | That's Caribou Island, Captain. Off the starboard bow, low and dark. We'll leave it well to starboard. | Pointing out the window. Matter of fact. *(in the room)* |
| `idle_mate_01` | Glass is still dropping. Twenty-nine twenty-four and going. | Reading the barometer. *(in the room)* |
| `idle_mate_02` | Lowest I've seen it on this lake. | Quiet. *(in the room)* |
| `idle_mate_03` | She's riding it, Captain. Heavy, but she's riding it. | Reassuring himself as much as you. *(in the room)* |
| `idle_mate_04` | Snow's closing in again. Can't see the stern from here. | Looking aft. *(in the room)* |
| `idle_mate_05` | Those hatch clamps had better be holding. That's green water going over them. | Watching the deck. *(in the room)* |
| `idle_mate_06` | Another couple of hours and we'll have Whitefish under our lee. | Hopeful. *(in the room)* |
| `idle_mate_night_01` | Dark as the inside of a cow out there. Not a light anywhere. | Low. *(in the room)* |
| `idle_mate_night_02` | We'd be raising Whitefish light by now. If it was lit. | Bitter. *(in the room)* |

## Wheelsman  (8 lines)

*Standing beside you. Few words.*

| File | Line | Direction |
|---|---|---|
| `wheel_ack` | Steady as she goes, aye, Captain. | Right beside you. *(in the room)* |
| `wheel_return` | You have the wheel, Captain. | Stepping back. *(in the room)* |
| `wheel_takes_01` | I have the wheel, Captain! Caribou's close aboard. Bringing her hard around. | Sharp, loud. The one time he raises his voice. *(in the room)* |
| `wheel_clear_01` | She's clear, Captain. Steadying up on one-four-one for Whitefish. Your wheel. | Breathing out. Back to normal. *(in the room)* |
| `idle_wheel_01` | She wants to fall off to starboard every time one comes under the quarter. | Working the wheel. *(in the room)* |
| `idle_wheel_02` | Holding one-four-one, Captain. Give or take. | Dry. *(in the room)* |
| `idle_wheel_03` | Felt that one through the wheel. | After a big sea. *(in the room)* |
| `idle_wheel_night_01` | Seas are coming higher than the pilothouse now, Captain. I've never seen that. | Quiet awe. *(in the room)* |

## Chief Engineer  (12 lines)

*On the sound-powered phone from the engine room. Gruff, practical, engine noise behind him.*

| File | Line | Direction |
|---|---|---|
| `chief_0305_01` | Engine room. She's running fine, Captain, but the wheel's coming out of the water when the stern lifts. Shaft's racing on us. | Phone line, engine noise behind him. Practical, a bit gruff. *(phone)* |
| `chief_0520_01` | Captain, engine room. Pumps are working hard. Whatever's coming in, it's coming in steady. | Not panicking. Not happy. *(phone)* |
| `chief_0630_01` | Captain... we've got water in the tunnel. Not much yet. Pumps are keeping up. For now. | The first crack in his voice. *(phone)* |
| `chief_pumps_ok` | Both pumps, aye. Pumping now. | Quick. *(phone)* |
| `chief_pumps_off` | Securing the pumps... aye, Captain. | Doubtful pause before "aye". *(phone)* |
| `chief_engine_ok` | Steady as she goes down here, Captain. Shaft races when the stern lifts, but she's holding. | Phone. Engine noise. *(phone)* |
| `chief_engine_list` | She's fine down here. I can feel that list though. Everything's leaning. | Phone. *(phone)* |
| `chief_engine_late` | We're making turns. Don't know how much longer the pumps can keep up. | Phone. Tired. *(phone)* |
| `galley_coffee` | I'll send the porter up. If he can make it along the tunnel. | A dry laugh. *(phone)* |
| `idle_chief_01` | Engine room. All's well down here, Captain. She's turning a hundred and twelve and burning clean. | Routine report. *(phone)* |
| `idle_chief_02` | Bridge, engine room. We've got water coming down the tunnel from somewhere forward. Pumps are keeping up. For now. | Measured. The "for now" is the point. *(phone)* |
| `chief_end_01` | Bridge! We're taking water in the tunnel, it's coming down the... it's over the... | Shouting over machinery. Cuts off mid-word. *(phone)* |

## Deck Watch  (12 lines)

*On the phone from aft, shouting over the wind. Youngest voice aboard.*

| File | Line | Direction |
|---|---|---|
| `watch_0330_01` | Captain, deck watch. There's a fence rail down on the starboard side. And two of the vent caps are gone off the ballast tanks. | Shouting into the phone, wind howling. Young-ish. *(phone)* |
| `watch_0330_02` | And Captain... she's got a list on her. To starboard. I can see it from back here. | Lower. Worried. *(phone)* |
| `watch_0545_01` | Captain! That one came clean over the hatches! Whole deck aft is under water! | Shouting, wind and water roaring. *(phone)* |
| `watch_sound_dry` | Tanks are dry, Captain. Nothing in them you wouldn't expect. | Bored. *(phone)* |
| `watch_sound_some` | Taking some water in the starboard tanks, Captain. Two, three feet. | Concerned. *(phone)* |
| `watch_sound_bad` | There's water in more tanks than I'd like, Captain. Six, eight feet in some. Pumps aren't gaining. | Worried. *(phone)* |
| `watch_sound_worse` | It's coming in faster than we can get it out, Captain. | Scared, trying not to be. *(phone)* |
| `watch_damage_none` | All secure, Captain. Hatch clamps are holding. Lot of water coming over, but she's tight. | Shouting over wind. *(phone)* |
| `watch_damage_rail` | Fence rail's down on the starboard side, couple of vent caps gone. Still taking water over the deck. | Shouting over wind. *(phone)* |
| `watch_damage_heavy` | I can't hardly see the deck aft, Captain. It's green water over the hatches every other sea. | Shouting, rattled. *(phone)* |
| `idle_watch_01` | Bridge, after house. Spray's freezing on the rails back here. Nobody's going out on that deck. | Shouting over the wind. *(phone)* |
| `watch_end_01` | Captain, the whole deck's gone under! She's not coming up! She's not... | Panic. Cuts off. *(phone)* |

## Captain of the Arthur M. Anderson  (20 lines)

*On VHF from ten miles astern. An old friend; easy radio manner.*

| File | Line | Direction |
|---|---|---|
| `and_0252_01` | Fitzgerald, this is the Anderson. You on here? | The captain of the ship ten miles astern. Old friend. Relaxed. *(VHF radio)* |
| `and_0252_02` | We're seeing gusts to fifty back here. Seas twelve, fifteen feet and building. How's she riding? | Casual concern. *(VHF radio)* |
| `and_0252_03` | Roger that. We'll be right behind you. Anderson clear. | Easy. *(VHF radio)* |
| `and_dmg_01` | Charlie on that, Fitzgerald. Do you have your pumps going? | A beat of concern under the calm. *(VHF radio)* |
| `and_dmg_02` | We'll stay with you. Anderson standing by on sixteen. | Steady. *(VHF radio)* |
| `and_dmg_late_01` | Fitzgerald, Anderson. You're riding awful low from what I can see back here. Everything all right up there? | Checking in, not alarmed yet. *(VHF radio)* |
| `and_radar_01` | We'll do that, Fitzgerald. We've got you on the scope, about ten miles ahead of us. We'll keep you posted. | Reassuring. *(VHF radio)* |
| `and_radar_late_01` | Fitzgerald, this is the Anderson. How are you making out up there? | Friendly. *(VHF radio)* |
| `and_nav_01` | Fitzgerald, Anderson. You're bearing one-four-zero from us. Whitefish is about twenty-five miles off your bow. Steer about one-four-one. | Reading off the radar. Precise. *(VHF radio)* |
| `and_0645_01` | Fitzgerald, Anderson. We just took two big ones over the stern. Water up over the hatch covers. Watch yourself, they're headed your way. | Shaken, quickly recovering. *(VHF radio)* |
| `and_final_01` | Fitzgerald, this is the Anderson. Have you checked down? There's a target nineteen miles ahead of us, you'll clear it. By the way, how are you making out with your problems? | Routine. Friendly. He has no idea. *(VHF radio)* |
| `and_final_02` | Okay, fine. I'll be talking to you later. | Casual sign-off. *(VHF radio)* |
| `and_checkin_early` | Rough, but we're making it. You're about ten miles ahead of us. | Easy. *(VHF radio)* |
| `and_checkin_mid` | Taking green water over the deck back here. You're still showing ten miles ahead. | Tense. *(VHF radio)* |
| `and_checkin_late` | Worst I've ever seen it. Hang in there, Fitzgerald. | Grim. *(VHF radio)* |
| `and_mayday_01` | Fitzgerald, Anderson. We heard that. We're coming up behind you as fast as she'll go. | Urgent. A friend. *(VHF radio)* |
| `and_offcourse_01` | Fitzgerald, Anderson. I show you well off the line to Whitefish. You having steering trouble up there? | Concerned, friendly. *(VHF radio)* |
| `and_offcourse_02` | Fitzgerald, Anderson. You're still wandering. Whitefish is one-four-one from you. Come on back to it, Captain. It's no night to be out there on your own. | Firmer. He's worried now. *(VHF radio)* |
| `idle_and_01` | Fitzgerald, Anderson. How's she riding up there? | Easy radio manner. *(VHF radio)* |
| `idle_and_02` | Fitzgerald, Anderson. We just took a big one over the stern. Lost a lifeboat cover. You watch yourself up there. | Shaken a little, hiding it. *(VHF radio)* |

## Coast Guard Group Sault Ste. Marie  (7 lines)

*Young radioman, by the book.*

| File | Line | Direction |
|---|---|---|
| `cg_beacon_01` | Fitzgerald, Coast Guard Group Sault. Our monitor shows the Whitefish beacon is off the air. Power's out on the point. We'll get somebody on it. Over. | Radioman, young, by the book. *(VHF radio)* |
| `cg_secur_01` | Sécurité, sécurité, sécurité. This is Coast Guard Group Sault Ste. Marie. Storm warnings remain in effect for all of Lake Superior. Northwest winds fifty to sixty knots, higher in gusts. Waves eighteen to twenty-five feet. All vessels seek shelter. Sault Coast Guard out. | Pronounced "say-cure-ee-tay". Official broadcast, flat and deliberate. *(VHF radio)* |
| `cg_weather_01` | Fitzgerald, Sault Coast Guard. Storm warnings in effect. Northwest winds fifty knots, gusts to seventy. Seas fifteen to twenty-five feet. Over. | By the book. *(VHF radio)* |
| `cg_mayday_early` | Edmund Fitzgerald, Coast Guard Group Sault. We copy your Mayday. Say the nature of your distress. Over. | Caught off guard, then professional. *(VHF radio)* |
| `cg_mayday_01` | Fitzgerald, Sault. We have nothing that can get out to you in these seas. We're asking for an aircraft out of Traverse City. Nearest vessel to you is the Anderson. Over. | Professional, helpless. *(VHF radio)* |
| `cg_mayday_repeat` | Fitzgerald, Coast Guard. We copy your Mayday. Aircraft is being readied. Hold on, Fitzgerald. | Strained. *(VHF radio)* |
| `cg_weather_02` | Fitzgerald, Coast Guard Sault. Latest from Marquette: winds northwest fifty-five to sixty-five knots, seas eighteen to twenty-five feet, snow, visibility near zero. Storm warning remains in effect. Over. | By the book. *(VHF radio)* |

## Pilot aboard the Avafors  (3 lines)

*A Great Lakes pilot on a foreign ship near Whitefish. Plain-spoken Midwesterner.*

| File | Line | Direction |
|---|---|---|
| `ava_01` | Fitzgerald, this is the Avafors. We're just coming around Whitefish now. The light's out and the beacon's out too. It's a mess out here. | A Lakes pilot on a foreign ship. Plain-spoken Midwesterner. *(VHF radio)* |
| `ava_02` | We're getting it too. Seas are twenty-five feet out here. Good luck to you, Fitzgerald. | Sober. *(VHF radio)* |
| `ava_call_01` | Fitzgerald, Avafors. Heard you asking about Whitefish. The light's back on now, but the beacon's still out. How are you making out? | Helpful. *(VHF radio)* |

## WGLR 1230 newsreader  (8 lines)

*1970s small-market AM news voice.*

| File | Line | Direction |
|---|---|---|
| `news_01` | This is WGLR, twelve-thirty on your dial. The National Weather Service has upgraded gale warnings to storm warnings for all of Lake Superior. Northwest winds thirty-five to fifty knots, waves eight to fifteen feet. | 1970s small-town AM newsreader. Warm baritone, slightly fast. *(AM broadcast)* |
| `news_02` | Here's the marine forecast. Storm warnings, Lake Superior. Northwest winds forty-five to sixty knots tonight. Waves twelve to eighteen feet, occasionally higher. Snow showers, visibility under a mile at times. | Reading a forecast. *(AM broadcast)* |
| `news_03` | Around the Soo this afternoon, high winds have torn down power lines and toppled trees. Highway crews are asking folks to stay home if they can. | Local news. *(AM broadcast)* |
| `news_04` | Power is out across parts of Chippewa County this evening as the storm moves through. Utility crews say it may be morning before everyone's back on. | Evening news. A bit tired. *(AM broadcast)* |
| `news_05` | The Weather Service says the worst of it should come through tonight, with winds easing tomorrow afternoon. Stay warm out there, and keep a thought for the boys out on the lake. | Signing off a segment. Sincere. *(AM broadcast)* |
| `news_06` | The National Weather Service has upgraded its gale warning for Lake Superior to a storm warning, with winds to sixty knots and seas of twenty feet or more. All shipping is advised to seek shelter. | Flat bulletin voice. *(AM broadcast)* |
| `news_07` | In Sault Ste. Marie, the Coast Guard reports several vessels anchored in Whitefish Bay riding out tonight's storm. Power is out across much of the eastern Upper Peninsula. | Flat. *(AM broadcast)* |
| `news_08` | The forecast for the Soo: snow tonight, heavy at times, with northwest winds gusting past fifty. Temperatures in the twenties. Drive carefully out there. | Winding down. *(AM broadcast)* |

## CKSM 850 disc jockey  (4 lines)

*Cheerful Canadian AM DJ.*

| File | Line | Direction |
|---|---|---|
| `dj_01` | Eight-fifty, CKSM, Sault Ste. Marie, Ontario. It's a wild one out there tonight, folks. Here's a little something to keep the lights on. | Canadian radio DJ, cheerful, a little hokey. *(AM broadcast)* |
| `dj_02` | CKSM. Phone lines are lit up tonight, half of you have lost power and the other half are asking us about it. Hang in there. | Chummy. *(AM broadcast)* |
| `dj_03` | Okay, it's eight-fifty on your dial, and if you're out on the big lake tonight, friend, this one's for you. Keep her pointed into it. | Warm, a little too cheerful. *(AM broadcast)* |
| `dj_04` | That was a little something to warm you up. Temperature here at the station is thirty-one degrees and dropping. Snow in the forecast. Who'd have guessed. | Chuckling at himself. *(AM broadcast)* |

## 1360 late-show host  (3 lines)

*Soft, slow, a little uncanny. The easter-egg station.*

| File | Line | Direction |
|---|---|---|
| `nightdj_01` | You're listening to the late show. Got a request in tonight for a song about a big ore boat on Superior. Looked all through the stacks. Can't find it anywhere. | Late-night DJ, soft, a little uncanny. Slow. *(AM broadcast)* |
| `nightdj_02` | Maybe nobody's written it yet. Here's something quiet instead. For everybody out on the big lake tonight. | Soft, kind. *(AM broadcast)* |
| `nightdj_03` | If you're hearing this, you're a long way from anywhere. Stay with me. I'll keep the lights on. | Soft. Almost a whisper. *(AM broadcast)* |

