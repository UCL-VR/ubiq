## Making an Object Graspable

If you want to have your object being grabbable with the user&#39;s hands, you will have to inherit from IGraspable and implement Grasp(...) and Release(...)

## Making an Object Usable

If you want your object to do something when the user presses the trigger button while holding it, you need to inherit from IUsable and implement Use(...) and UnUse(...)