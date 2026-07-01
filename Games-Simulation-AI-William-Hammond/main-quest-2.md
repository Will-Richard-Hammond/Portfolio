# Main Quest 2

[← Main Quest 1](main-quest-1.md) | [Back to Main README](README.md) | [Next: Main Quest 3 →](main-quest-3.md)

SMART Objectives
The SMART objectives are objectives that are specific, measurable, achievable, relevant and time bound. At the start of each lab in the Main Quest SMART objectives will be set out for the lab. As SMART objectives are specific, measurable, achievable, relevant and time bound they are a great way to evaluate the success (or otherwise) of the lab at the end of the lab. This approach is taken to help support your dissertation project - where you may want to do a similar thing. Having a good set of SMART objectives at the start of a project can help you evaluate the success of your project at the end.

The SMART objectives for this lab are as follows:

By the end of this lab, students will be able to implement and respond to mouse clicks inside four shapes (circle, square, rectangle, and triangle).
By the end of this lab, students will be able to animate variables such as position, rotation, and colour using a parametric approach within a MonoGame project.
By the end of this lab, students will be able to explain the purpose of MonoGame’s GameComponent class and integrate it into a simple project to structure game logic and behaviour.
By the end of this lab, students will be able to design and implement a reusable game utility library that can support reusing common functionality across multiple games.


## Notes

<!-- Add any additional notes, reflections, or observations here -->
game1 inherits from Game which has a 1 to many relationship with GameComponents
by using the abstract stroopshape class it allows for inheritance to more specific classes and enforces the implementation of the IsInside method for hit detection as well as common properties for stroopshapes.
the different shapes inhereit from the parent stroopshape but have thier own logic for hit detection reflecting their geometric differences.
the game treats all shapes the same in updates and draw loops relying on polymorphic behaviour for shape specific checks.

---

**Navigation:**
- [Main README](README.md)
- [Main Quest 1](main-quest-1.md)
- [Main Quest 3](main-quest-3.md)
