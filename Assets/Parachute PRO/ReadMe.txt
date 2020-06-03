Parachute PRO - by Gagik Khachatryan

v.1.0

thanks for purchase !


-------- Usage ---------

* Note - for in detail information, after reading this instruction, open and test - Assets/Parachute PRO/Demo/DemoScene

1) Find the 'Parachute' prefab in Assets/Parachute PRO/_PREFAB
2) Drag-Drop (or Instantiate) 'Parachute.prefab' into your scene.
3) Place 'Parachute' gameobject into your 'Character' gameobject (Drag-Drop in hierarchy view).
4) (Optional) Inside your script (e.g. GameManager) ignore collision between character and backpack `
        Physics.IgnoreCollision(collCharacter, collBackpack, true);
5) Create in your scene capsule colliders as ground zone for parachute.
6) Call these methodes to control your parachute `
		parachute.Open();
		parachute.Drop();