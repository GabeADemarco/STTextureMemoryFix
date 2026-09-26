# Investigation and Findings

## Symptom

After loading a store, Supermarket Together could commit roughly 12.5–12.9 GB of process memory on the test system while its resident working set remained much smaller. With no Windows page file, this consumed a large portion of the system commit limit.

## What was ruled out

The large allocation reproduced with BepInEx disabled, ruling out the existing mods as its source. Disabling autosave did not remove it. PlayMaker handler counts remained stable, and the managed GC heap was only a small fraction of total process commit.

## VMMap

VMMap showed that the gameplay increase was overwhelmingly private committed data rather than ordinary managed/CRT heap. The allocation was distributed across many private read/write regions.

## WPR / WPA

A Windows Performance Recorder VirtualAllocation trace captured while loading a store showed the dominant allocation path originating in `UnityPlayer.dll` and reaching Direct3D 11 texture creation, including:

- `d3d11.dll!CDevice::CreateTexture2D`
- `d3d11.dll!CDevice::CreateTexture2D_Worker`

About 9.5 GB of outstanding process commit in that trace was associated with the texture-creation branch. This should not be interpreted as 9.5 GB of physically resident VRAM; graphics-resource backing can consume Windows commit without appearing as process working set.

## Unity texture inventory

A temporary diagnostic plugin measured approximately:

- 8.52 GiB reported Unity texture memory
- ~4,146 Texture2D objects
- ~8.54 GiB estimated Texture2D memory from dimensions/formats/mips
- ~0.93 GiB streaming Texture2D
- ~7.6 GiB non-streaming Texture2D

Many large assets were 4096x4096 textures with full mip chains. The game's normal graphics presets did not materially reduce this texture footprint.

## Controlled fix

Setting `QualitySettings.globalTextureMipmapLimit` before store resources loaded produced:

- **0 / vanilla:** ~12.5–12.9 GB process commit
- **1 / half:** ~5.2–5.4 GB process commit
- **2 / quarter:** ~3.5–3.6 GB process commit

Mode 1 was exercised for roughly a full in-game working day and remained stable, with no meaningful visual degradation noticed during that test. Mode 2 remained functional but visibly degraded some screens and machines.

Unity's high-level texture counter continued to report about 8.52 GiB after changing the mip limit even though Windows process commit fell dramatically. For this experiment, Windows process commit was therefore the useful measurement of the backing-memory effect.

## Runtime switching

The release plugin was then tested switching repeatedly among 0, 1 and 2. The Unity property changed successfully in both directions. Moving toward higher quality can take noticeably longer to settle because texture resources need to be recreated or repopulated.

## Conclusion

The observed behavior was not a conventional continuously growing managed-memory leak. The dominant memory event occurred during gameplay initialization and correlated with Unity/D3D11 texture-resource creation.

Reducing the global mip level for eligible textures substantially reduced Windows committed memory on the test system. Mode 1 provided the best tested balance and is therefore the default.
