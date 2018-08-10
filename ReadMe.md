# Task Chain

## What?

A lock-less parallel library. (Ok, nearly lockless, atleast no locks on hot paths.) 

## Why?

It fills a few holes in `System.Collections.Generic`. Specifically, it has threadsafe list, a threadsafe set and its dictionary implementation with thread safe delegate methods.

## Types 

* **`ConcurrentArrayList`** - A list that is thread safe but you cannot remove items. 

* **`ConcurrentSet`** - A threadsafe Set.

* **`ConcurrentIndex`** - A threadsafe dictionary. Unlike `ConcurrentDictionary`, `ConcurrentIndex` methods that accept delegates are thread safe. However it does lack may of `ConcurrentDictionary` features like `Count` and `Remove`.

* **`QueueingConcurrent<T>`** - Makes an object thread safe by queing code that modify it.

* **`RawConcurrentIndexed`** - Extremely light version of `ConcurrentIndex`. `RawConcurrentIndexed` Is read only and does not provide `Count`. 
Enumeration is safe but might not include items added during enumeration.

* **`RawConcurrentArrayList`** - Extremely light version of `ConcurrentArrayList`. `RawConcurrentArrayList` does not support `Set` and 
enumeration might not include items added during enumeration.

* **`RawConcurrentLinkedList`** - A minimal singly linked list. 

## Great! I am going to use it!

I wouldn't, atleast not in production. It was fun to make, but I have not really tested it enought and it lacks basic features.

