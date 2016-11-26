# LambdaTest

I wanted to learn to use Func<T, TResult> in methods, so i can use lambda in my own methods. 

It's supposed to create a expression from user input, and then construct a custom func expression from filtered input.

I ended up making it a bit bigger than i intended. There's no architectonic correctness - actually, it's a mess! Maybe in a iteration I will look into improving it, by introducing multiply files, and introducing some kind of patterns so its not so loosly coupled.

The search algorithm is made from logic, there's no mathmathic patterns, maybe in a later iteration, I'll also introduce this - it depends on time.

I have to make some more test data, since it's from memory.

Features:
Search for a person with multiply conditions
Supports operators: >, < and |
ex. Birthday > 23-11-1993 &  Name = Patrick
ex. Age > 19 | Birthday < 01-01-1989

Currently a console application, but since filtering is introduced, it could easily be made into a WPF since theres not really much design logic which is only in the Run(), the rest doesnt depend on anything but business logic

