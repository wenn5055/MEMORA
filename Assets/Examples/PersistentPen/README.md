# Persistent Pen

## Description

This example implements a simple pen which can make up to 20 synced, colored lines and an eraser which is highlights 
and deletes lines drawn by the local player.

By putting a `VRC Player Object` component on the `SimplePenSystem`, it is automatically spawned for each player in the 
world (and removed when they leave). All synced properties of the pen system are automatically saved and loaded - like 
its position and the points and colors of each line.

---
## How to Use This Example

Play the scene, pick up the pen, and tap `Use` without moving the pen much to cycle through the available colors. 
Hold `Use` and move the pen to make lines in 3D space, release `Use` to finish the line.

Drop the pen and pick up the eraser, then stick the eraser directly into any of your lines. 
Known Issue: In ClientSim, this does not highlight the line - it works in the VRChat Client. 
Press `Use` to delete the selected line - this adds it back to your collection of 20 lines.

Stop the scene and then restart it to see your lines restored!

## Things to Play With

## Udon Pen
Check out the `Udon Pen` script in the scene under `SimplePenSystem/Pen`. 

- Change the gradient used as the `Palette Color` to swap the available colors

## Lines
You can find the Lines used by the pen under `SimplePenSystem/Lines`.

- If you want more lines available per pen, just duplicate some of the existing lines! No other changes needed
- You can make changes per-line if you want, like setting different widths or materials

## Roadmap

- Respawn the pen whenever it's more than X units away from you
- New color picker, one where you hold a button to show a palette, then move to the color you want to use
- Refactor to allow infinite lines
