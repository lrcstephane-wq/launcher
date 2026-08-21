# Plan de tests

Ce plan est à exécuter sur Windows avant chaque publication stable.

## Démarrage et migration

- lancer l'exécutable sans aucun dossier `%APPDATA%\Ideo\Launcher` ;
- vérifier la création du catalogue et des cartes TopSolid détectées ;
- fermer puis rouvrir et vérifier que les données sont conservées ;
- lancer depuis la version précédente et vérifier que la mise à jour ne supprime aucune donnée.

## Cartes

- créer une carte avec titre, cible, arguments, couleur, logo et plusieurs tags ;
- tester puis enregistrer la commande ;
- modifier le titre et le groupe ;
- dupliquer puis supprimer la copie ;
- importer un `.lnk` par le bouton et par glisser-déposer ;
- vérifier une carte avec cible absente : message visible et lancement désactivé ;
- lancer normalement puis en administrateur ;
- vérifier le comportement avec et sans réduction automatique.
- ajouter des accès rapides par sélection et par glisser-déposer ;
- ouvrir un dossier local et un partage réseau valide sans message de succès ;
- vérifier l'alerte sur un dossier absent et la copie du chemin ;
- vérifier un chemin avec variable d'environnement et un chemin relatif au catalogue.

## Organisation et filtres

- créer, renommer, déplacer puis supprimer un groupe ;
- réordonner les cartes par menu et glisser-déposer ;
- ajouter/retirer un favori et lancer plusieurs cartes pour contrôler l'ordre des récents ;
- sélectionner deux tags dans une catégorie et confirmer le `OU` ;
- sélectionner une seconde catégorie et confirmer le `ET` ;
- combiner recherche texte et filtres ;
- enregistrer, rappeler puis supprimer une vue ;
- vérifier que la vue apparaît immédiatement après validation ;
- combiner « Favoris uniquement » avec des tags puis enregistrer la vue ;
- masquer puis réafficher les filtres et contrôler la persistance au redémarrage ;
- replier un groupe et contrôler la persistance au redémarrage ;
- vérifier les modes détaillé et compact.

## Interface

- vérifier la date française dynamique dans l'en-tête ;
- vérifier les fenêtres Nouveau groupe et Enregistrer la vue avec un libellé court puis long ;
- vérifier les ascenseurs de toutes les fenêtres et les menus `•••`/clic droit en thème sombre ;
- ouvrir la page de téléchargements TopSolid depuis le panneau gauche et depuis le menu principal ;
- créer un tag en choisissant une catégorie existante, puis en saisissant une nouvelle catégorie.

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

## Mise à jour

- publier une release de test contenant exactement `Launcher.exe` ;
- vérifier la détection de version, la progression, le contrôle SHA-256, le remplacement et le redémarrage ;
- vérifier le journal après une mise à jour réussie et après une erreur simulée.
