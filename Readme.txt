Playing with PLINQ Performance using the StackOverflow Data Dump

Right off the bat, I’d like to stress that adding a .AsParallel() to your code won’t magically speed it up. Knowing this I still had unrealistic expectations when I began creating a demo to specifically show performance improvements.

This demo is up here on Git (soon).