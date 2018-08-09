Ok so this is a big mess. I need to rethink it a bit

Here is the new plan:
* Make a concurrent<T> this class makes an object thread safe
   * Updated using interlock
   * Actions<T> are queued and then stoped at a ManualResetEventSlim  
   * Reads return the current value 
* Data types hold concurrents


