# Publication d'une version

## Vérification

1. travailler sur une branche `feature/...` ou `fix/...` ;
2. attendre le succès du workflow **Build Windows**, y compris l'étape **Regression checks** ;
3. télécharger l'artifact et effectuer un test Windows : ouverture, création d'une carte, lancement, fermeture/réouverture ;
4. fusionner sur `main` uniquement après validation.

## Version

Mettre à jour dans le fichier projet :

- `Version` ;
- `FileVersion` ;
- `AssemblyVersion`.

Le tag GitHub doit utiliser le même numéro précédé de `v`, par exemple `v0.4.0`. L'updater compare ce tag à la version embarquée dans `Launcher.exe`.

## Release GitHub

La release doit contenir un asset nommé exactement `Launcher.exe`, car ce nom est utilisé par `UpdateService`. Après publication :

1. ouvrir une version précédente du launcher ;
2. cliquer sur **Mise à jour** ;
3. vérifier le téléchargement, le remplacement de l'exécutable et le redémarrage ;
4. vérifier que le catalogue et les préférences sont conservés.

Pour une évolution du format du catalogue, le test de migration doit repartir d'un JSON représentatif de la version publiée précédente et vérifier chaque champ partagé avant la fusion.

Ne jamais supprimer les données utilisateur lors d'une mise à jour : l'exécutable et les fichiers `%APPDATA%`/`%LOCALAPPDATA%` ont des cycles de vie séparés.
