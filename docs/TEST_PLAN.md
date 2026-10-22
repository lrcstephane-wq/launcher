# Plan de tests

Ce plan est à exécuter sur Windows avant chaque publication stable.

## Démarrage et migration

- lancer l'exécutable sans aucun dossier `%APPDATA%\Ideo\Launcher` ;
- vérifier la création du catalogue et des cartes TopSolid détectées ;
- fermer puis rouvrir et vérifier que les données sont conservées ;
- ouvrir un catalogue 0.3.2 comportant plusieurs cartes configurées ;
- vérifier pour chaque carte la conservation de l'identifiant, du titre, du groupe, des tags, de l'ordre, du logo, de la couleur, de la cible, des arguments, du dossier de travail, des accès rapides et des options ;
- vérifier que l'ancienne commande devient un seul mode **Par défaut** et qu'une sauvegarde du JSON 0.3.2 est créée avant la première écriture ;
- exécuter le projet `Ideo.TopSolidLauncher.RegressionChecks` et attendre `Migration v0.3.2 -> v0.4.0 : OK`.

## Cartes

- créer une carte avec titre, cible, arguments, couleur, logo et plusieurs tags ;
- tester puis enregistrer la commande ;
- modifier le titre et le groupe ;
- dupliquer puis supprimer la copie ;
- importer un `.lnk` par le bouton et par glisser-déposer ;
- vérifier une carte avec cible absente : message visible et lancement désactivé ;
- lancer normalement puis en administrateur ;
- vérifier le comportement avec et sans réduction automatique.
- créer les modes `Local` et `Distant · Nextcloud`, les réordonner, les dupliquer et changer le mode par défaut ;
- lancer le mode choisi dans la flèche, redémarrer puis vérifier que le bouton principal réutilise ce mode uniquement sur le poste courant ;
- vérifier qu'un mode inaccessible reste listé mais désactivé ;
- ajouter des accès rapides par sélection et par glisser-déposer ;
- ouvrir un dossier local et un partage réseau valide sans message de succès ;
- vérifier l'alerte sur un dossier absent et la copie du chemin ;
- vérifier un chemin avec variable d'environnement et un chemin relatif au catalogue.

## Organisation et filtres

- créer, renommer, déplacer puis supprimer un groupe ;
- réordonner les cartes par menu et glisser-déposer en modes détaillé et compact ;
- saisir une carte depuis toute zone non interactive, vérifier l’animation des cartes voisines et le repère bleu avant/après ;
- confirmer que les boutons, cases à cocher et menus d’une carte restent cliquables sans déclencher un déplacement ;
- ajouter/retirer un favori et lancer plusieurs cartes, puis confirmer qu'aucune carte ne change de place automatiquement ;
- réordonner les catégories et les tags avec leurs poignées ;
- saisir une catégorie puis un tag depuis toute la largeur de leur ligne, vérifier le déplacement animé et le repère avant/après ;
- annuler un déplacement avec `Échap` et vérifier que l’ordre initial est restauré ;
- sélectionner deux tags dans une catégorie et confirmer le `OU` ;
- sélectionner une seconde catégorie et confirmer le `ET` ;
- combiner recherche texte et filtres ;
- enregistrer, rappeler puis supprimer une vue ;
- vérifier que la vue apparaît immédiatement après validation ;
- combiner « Favoris uniquement » avec des tags puis enregistrer la vue ;
- masquer puis réafficher les filtres et contrôler la persistance au redémarrage ;
- replier un groupe et contrôler la persistance au redémarrage ;
- vérifier les modes détaillé et compact.
- sélectionner plusieurs cartes et tester le changement de groupe, les tags, l'accès rapide commun et le remplacement de racine ;
- ouvrir `Ctrl+K`, rechercher une carte et un mode, naviguer au clavier et lancer avec `Entrée`.

## Interface

- vérifier la date française dynamique dans l'en-tête ;
- vérifier les fenêtres Nouveau groupe et Enregistrer la vue avec un libellé court puis long ;
- vérifier les ascenseurs de toutes les fenêtres et les menus `•••`/clic droit en thème sombre ;
- ouvrir la page de téléchargements TopSolid depuis le panneau gauche et depuis le menu principal ;
- créer un tag en choisissant une catégorie existante, puis en saisissant une nouvelle catégorie.
- vérifier les icônes des menus principal et cartes, ainsi que le logo fusée dans l'en-tête, la fenêtre et la barre des tâches.
- déposer plusieurs raccourcis `.lnk`, décocher une ligne puis importer la sélection.

## Données et partage

- exporter puis importer un catalogue ;
- restaurer une sauvegarde depuis `Backups` ;
- créer une copie dans un dossier partagé et contrôler les logos sur un second poste ;
- modifier le catalogue depuis un second poste et vérifier le rechargement automatique sur le premier ;
- ouvrir une fenêtre d'édition, modifier le catalogue depuis un second poste et vérifier que le rechargement attend la fermeture de la fenêtre ;
- démarrer avec le partage indisponible, vérifier que le chemin est conservé, puis rétablir le réseau et utiliser **Recharger maintenant** ;
- revenir au catalogue local et vérifier que le catalogue partagé n'est pas supprimé ;
- recharger le catalogue avec `F5` ;
- redétecter TopSolid sans créer de doublons.
- créer la ressource `Licences TopSolid` avec les emplacements Local et Distant · Nextcloud ;
- vérifier que le bouton principal mémorise localement le dernier emplacement et que la flèche ouvre les deux choix ;
- lancer **Vérifier le catalogue** avec des chemins valides puis invalides ;
- contrôler l'historique après plusieurs modifications et restaurer une sauvegarde.

## Mise à jour

- publier une release de test contenant exactement `Launcher.exe` ;
- vérifier la détection de version, la progression, le contrôle SHA-256, le remplacement et le redémarrage ;
- vérifier le journal après une mise à jour réussie et après une erreur simulée.
