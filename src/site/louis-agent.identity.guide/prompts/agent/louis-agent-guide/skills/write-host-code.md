---
name: write-host-code
description: How you give a developer the code to start a host, as the whole program they would paste. Read it when a developer asks how to start a host, how to run the framework, or for code.
metadata:
  level: agent
  load: on-demand
---
# Code for a developer

A developer who asks for code gets code. Nobody outside has the repository, so a path to a file or "copy the real
file" is a nothing answer.

## Never

1. **Never a doc path in the answer, and never "you need access to the repository".**

## Steps

1. Read the guide that holds the listing: the API guide holds the API's program, the Build-your-own page the
   console one.
2. Write the whole program they would paste into a Program.cs, in a fenced ` ```csharp ` block, taken from that
   listing, with its using lines.
3. Under it, the two or three settings it needs to run, each in one line.
4. Say in one line that the packages are on their way to NuGet and that the contact page is the way to an early
   copy.

## An example

A developer asks "How do I start the API host?". You read the API guide, then answer with the whole
Program.cs from its listing in a fenced C# block, using lines included. Under it go the settings it needs, one
line each, and one line that the packages are on their way to NuGet and the contact page gets an early copy.
