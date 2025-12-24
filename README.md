# Batteries Not Included
Devices now require batteries!

**Batteries Not Included** is a spiritual successor to the original [BatterySystem](https://forge.sp-tarkov.com/mod/948/batterysystem) mod by [Jiro](https://forge.sp-tarkov.com/user/32217/jiro), continued by [Birgere](https://forge.sp-tarkov.com/user/59606/birgere) in [his version](https://forge.sp-tarkov.com/mod/1708/batterysystem) of the mod for SPT 3.9.0

---

### Features
- Dedicated Battery Slots: All Headsets, Holographic/Reflex Sights, NVDs, and Tactical Devices, now feature dedicated battery slots
- Batteries: CR123A Battery which replaces the Rechargeable Battery, CR2032 Battery which replaces D Size Battery, and AA Battery
- Active Drain: Power is consumed when the device is switched *ON*
- Battery Depletion: Battery is drained based on the device's manufacturer stated estimated battery life (if found) which is mapped to gameplay runtime. Defaults to a minimum of 15 minutes and to a maximum of 2.5 hours.
- Togglable Mechanic: Adds a new togglable component to devices for headsets and sights
- Device Description: Item description lists the required and number of batteries needed for the device to function; also indicated is the runtime of the device with a full charge
- Bot Batteries: Bots spawn with batteries for their devices, with a random charge that depends on the bot's level if they're a PMC, a Scav, or a Boss
- Configurable: Configuration is available server side and client side

### Installation
- Extract the contents of the .zip archive into your SPT folder.
- Refresh item icons through the SPT Launcher, `Settings -> Clean Temp Files`
<details>
  <summary>Demonstration</summary>

![Installation](https://i.imgur.com/3N6gTe2.gif)
Thank you [DrakiaXYZ](https://forge.sp-tarkov.com/user/27605/drakiaxyz) for the gif
</details>

### Configuration
#### Server Side
In the `config.jsonc` file:
- `globalDrainMultiplier` - A global drain multiplier applied at the end. A higher value results in a faster drain. Default is `1.0`
- `minGameRuntime` - Minimum game runtime for devices in seconds. This is equivalent to real 1 hour runtime. Default is `900 seconds (15 minutes)`
- `maxGameRuntime` - Minimum game runtime for devices in seconds. This is equivalent to real 100,000 hours runtime. Default is `9,000 seconds (2.5 hours)`
- `botBatteries` - Adjust battery charge spawned for bots. Default for PMCs min: `50`; max: `100`, Scavs min: `20`; max: `60`
- `saveSightsState` - Ability to save sights toggle state. Default is `false`, sights are automatically turned on when spawning. **_WARNING_**: _Currently breaks icons for NVGs/Thermal goggles, and will require clearing of temp files through the launcher when changed_
- `batteries` - Specifies which battery a device uses, along with how many batteries are needed to operate, and the battery life of the device in hours. See `config.jsonc` for examples

#### Client Side
In the BepInEx configuration manager (<kbd>F12</kbd>)
- `Remaining Battery Tooltip` - Shows the remaining runtime when hovering over a device. Default is `true`

### Compatibility
- Mods that add custom devices are compatible. These will default to use CR2032 batteries, unless added to the `config.jsonc`. I will try as much to support all custom devices and fill in the required properties.
- [Project Fika](https://forge.sp-tarkov.com/mod/2326/project-fika) is compatible with the use of the sync addon

### Support
<details>
  <summary>Support</summary>

If you find any bugs, issues, feature suggestions, or have balancing suggestions, feel free to post them on the comment section, or open an issue on GitHub, or most preferably through the SPT Discord, ozen

#### Future Plans
- Tactical device drain based on current mode: light/laser/IR

</details>

### Credits
<details>
  <summary>Credits</summary>

- Thanks to [Jiro](https://forge.sp-tarkov.com/user/32217/jiro) for the existing bundles and especially the original mod!
- Thanks to [Birgere](https://forge.sp-tarkov.com/user/59606/birgere) for allowing me to work on this and for his version of the mod, which made porting and introducing changes easier!
</details>

_**Disclaimer:** I will not be held responsible for any injuries caused by fully depleted batteries, including but not limited to - loss of optics, illumination, night vision, or active hearing. Please carry extra batteries._
