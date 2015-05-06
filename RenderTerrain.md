# Introduction #

This project contains two implementions of Level Of Detail (LOD) terrain rendering. These were inspired by the following publications:

  * Discrete Level Of Detail Algorithm, as found on www.gamasutra.com, by Chris Dallaire, entitled "Binary Triangle Trees for Terrain Tile Index Buffer Generation".
  * Terrain Simplification Simplified: A General Framework for View-Dependant Out-of-Core Visualization, by Peter Lindstrom. This was referenced by Shamus Young, who wrote an OpenGL implementation.

I am currently using a 'home brew' implementation of parts of the Simplification algorithm, as I understand it. It is probably not a good rendition, but is sufficient at this stage for my needs.

It currently supports _very_ limited shading.. by combining 4 textures based on height. No texture maps per 'real world' terrain data.

# Links #
  * [Discrete LOD article](http://www.gamasutra.com/features/20061221/dallaire_01.shtml)
  * [Simplification article](http://www.shamusyoung.com/twentysidedtale/?p=141)

# Information #

[Screen Shots](ScreenShots.md)

# To Do List #
  * Better shader effects
  * improved camera logic
  * text for logging, fps, etc
  * text-based command menu

- Ben Laan