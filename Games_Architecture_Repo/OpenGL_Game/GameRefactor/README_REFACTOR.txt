Refactor staging area

This folder tree is a new modular separation layer and does not replace the all current game files yet.

Engine code:
- OpenGL_Game\\Engine\\Components
- OpenGL_Game\\Engine\\Entities
- OpenGL_Game\\Engine\\Systems
- OpenGL_Game\\Engine\\Managers
- OpenGL_Game\\Engine\\Input
- OpenGL_Game\\Engine\\Collision
- OpenGL_Game\\Engine\\Config

Game-specific refactor code:
- OpenGL_Game\\GameRefactor\\Managers
- OpenGL_Game\\GameRefactor\\Config

Purpose:
- keep current files unchanged
- begin separating reusable engine code from game-specific code
- provide abstract Input Manager and Collision Manager foundations
- provide reusable Entity, Component, System, EntityManager, and SystemManager foundations
- provide scene-facing input hookup through an engine bridge
- provide collision service wiring foundation
- provide settings loading foundation from file

- the refactor had reached near completion before needing to submit the work
