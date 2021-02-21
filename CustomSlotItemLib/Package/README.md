
# CustomSlotItemLib for Valheim

This is a Bepinex mod for Valheim that makes custom slotted items easier to develop.

## Development

### Obtaining a reference

- If you're using Visual Studio, add a reference to the dll under your project and make sure private is set to 'False' so that it doesn't get bundled with your mod.
- If you're using Unity Editor, you should be able to drop the dll into your assets folder and it'll get loaded.

### Applying a custom slot

If you are a developer wishing to leverage this mod, then you have two ways to do so:

Dynamically add the CustomSlotItem MonoBehaviour to the prefab's gameboject using the utility function, e.g.:

    using CustomSlotItemLib;
    
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    [HarmonyPostfix]
    static void PrefabPostfix(ref ZNetScene __instance)
    {
        GameObject wishbonePrefab = __instance.GetPrefab("Wishbone");
        CustomSlotManager.ApplyCustomSlotItem(wishbonePrefab, "wishbone");
    }

**OR:**

Add the MonoBehaviour to your asset so that it'll be loaded with the component already attached.

### Re-using slot name dictionary values

Here is a list of predefined slot name values that may be used:
- boots
- face
- ring
- neck
- bracers
- ears

If your desired slot name is not listed please use a brief camelcased name and contact me so that I can add it here.

## Planned

- Support for multiple items in a single slot, such as wearing 2 rings at once
- Support for items taking up more than one slot

## Installation (manual)

If you are installing this manually, do the following

1. Extract the archive into a folder. **Do not extract into the game folder.**
2. Move the contents of `plugins` folder into `<GameDirectory>\Bepinex\plugins`.
3. Run the game.
