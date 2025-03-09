# Autonomy

Give your colonists real agency. Let them make decisions based on their personalities, their environment, and their abilities.

# What Does This Mod Do?

Autonomy allows colonists to set their own work priorities using a variety of conditions, including their passions, stats, environmental factors, and special rules. These conditions output also varies depending on their personalities. The mod is designed for easy expansion and mod support, as each condition is directly linked to a work type through XML.

Personality is determined by work drive givers, which are attached to traits, genes, precepts, memes, and backstories (some of these are still a work in progress due to their large number). These personalities shape how a pawn approaches work in an ambiguous yet meaningful way. For example, a kind pawn will have a more communal work ethic, whereas a psychopath will be more individualistic. These work drives influence how each pawn interprets conditions and decides their actions.

The mod also introduces several new work types, subdividing existing ones similarly to the Complex Jobs mod. However, this means it is not compatible with Complex Jobs or other mods that add work type subdivisions. While these new work types are optional, playing with them enabled is recommended, as the mod’s balance is designed around them.

# Does everyone have Autonomy?

You can enable and disable autonomy individually for each pawn that shows up in the Work Tab. However, some have special rules.

Slaves with autonomy enabled will never use workdrive multipliers, wordrive base type conditions, and passion conditions. This means that they will only consider their stats and the colony needs. Maybe in the future I will revisit this to separate systems.

# How about having an ideology surrounding Autonomy desire

Maybe. Personally I prefer to play with Autonomy always on because I prefer my pawns adapt automatically

# Is This Mod Compatible With Other Mods?

Currently, this mod has no built-in compatibility, but future updates will add support for other mods. It is also designed to be easily made compatible through additional patches.

# How Is This Mod Different From Free Will?

Free Will is an excellent mod that heavily inspired Autonomy. The key motivation behind Autonomy was to build a more compatible and expandable foundation for a Free Will-like system, allowing for simple XML patches to extend its functionality. Unlike Free Will, Autonomy features a unique personality system that affect the conditions for determining work priorities. Additionally, it is designed for easy rebalancing, as all numerical values used in calculations are exposed and adjustable via XML patches. The mod also integrates seamlessly with the game’s existing UI, displaying most of its information within the Work Tab.

# TODO

* Add many WorkdriveGivers to precepts, traits, backstories, and genes
* Add a default priorityGiver that is controlled by settings, so players can force a specific worktype to be higher for everyone.
* Automatically enable the numeric priority system and permantently disable the check based one- I don't expect no one using this mod using the simpler system.
* Consider priority for more than capable
* Check possible incompatibilities with priorityHaving non human pawns like mechanoids or modded ones like VFE Phytokin
* Add better error catching so its easier to get which pawn is causing a problem

# Ideas (May or may not happen)

* Subdivide this mod into a library and main mod so we can build modules later (This needs to happen before the first realse if its gonna happen)
* Add schedule Autonomy (Module?)
* Add a condition builder in settings