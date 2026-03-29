# PEAK Class Mod

Mod pour le jeu **Peak** permettant d'ajouter et de personnaliser des classes de personnages avec des mécaniques avancées.

## Installation
1. Compiler le projet (voir ci-dessous)
2. Copier le fichier `.dll` généré dans le dossier `BepInEx/plugins` du jeu Peak
3. Lancer le jeu avec BepInEx

## Structure du projet
```
/src
  /Handlers      # Comportements attachés aux personnages/objets
  /Patches       # Patchs Harmony (modifications du jeu)
  /Utils         # Fonctions utilitaires
  ClassManager.cs
  ClassBalance.cs
  PeakClassModPlugin.cs
```

## Classes
- Medic
- Gobelin
- Tech Priest
- Mushroomancer
- Great Epee
- Rogue
- Astronaut



