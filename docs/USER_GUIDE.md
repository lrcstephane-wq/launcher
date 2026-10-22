# Guide utilisateur

## Créer une carte

Cliquer sur **Nouvelle carte** ou sur **Ajouter** dans un groupe, puis renseigner les informations communes : titre, description, groupe, couleur, logo et tags.

Ajouter ensuite autant de **modes de lancement** que nécessaire. Chaque mode possède :

- un nom, par exemple `Local` ou `Distant · Nextcloud` ;
- la cible à lancer ;
- les arguments éventuels ;
- le dossier de travail ;
- autant d'accès rapides que nécessaire, avec un libellé et un chemin de dossier ;
- le lancement administrateur et la réduction de la fenêtre si nécessaire.

Le bouton **Tester ce mode** permet de vérifier la cible sélectionnée avant d'enregistrer. Un mode peut être ajouté, dupliqué, supprimé, réordonné ou défini comme mode partagé par défaut.

Sur la carte, la partie gauche du bouton lance le dernier mode utilisé sur ce poste. La flèche à droite ouvre la liste complète des modes ; les modes indisponibles sont visibles mais désactivés avec la raison du problème.

Un raccourci Windows `.lnk` peut être importé avec le bouton prévu ou déposé directement dans la fenêtre. Plusieurs `.lnk` déposés ensemble ouvrent une liste de contrôle avant leur import en lot.

Un dossier peut aussi être déposé dans la fenêtre : il est ajouté aux **Accès rapides**. Les variables Windows comme `%USERNAME%`, les chemins réseau `\\serveur\partage` et les chemins relatifs au dossier du catalogue sont acceptés. La carte affiche les deux premiers accès ; tous restent disponibles dans son menu `•••`.

Cliquer sur l'icône de dossier teste d'abord le chemin. S'il existe, l'Explorateur s'ouvre sans message supplémentaire ; sinon, une alerte indique le chemin inaccessible.

## Organiser

- saisir une carte par sa poignée `⠿`, puis la déposer avant ou après une autre carte ; le geste fonctionne en vue détaillée et compacte ;
- glisser une carte dans un autre groupe pour la déplacer ;
- utiliser le menu `•••` d'une carte pour modifier, dupliquer ou supprimer ;
- utiliser le menu d'un groupe pour le renommer, le déplacer ou le supprimer ;
- cliquer sur l'étoile d'une carte pour la retrouver avec le filtre Favoris ;
- cliquer sur la flèche d'un groupe pour le replier ou le déplier.

L'ordre affiché reste toujours l'ordre manuel. Lancer une carte ou la placer en favori ne la déplace jamais.

Le bouton **Sélectionner** permet de cocher plusieurs cartes, puis d'appliquer en une seule fois un changement de groupe, l'ajout/retrait de tags, un accès rapide commun ou le remplacement d'une racine de chemin.

## Rechercher et filtrer

La barre de recherche inspecte les titres, descriptions, tous les modes, commandes et tags. Plusieurs cases peuvent rester cochées en même temps.
Elle inspecte également les libellés et chemins des accès rapides. Le filtre **Favoris uniquement** peut être combiné avec tous les autres filtres.

- dans `Version`, cocher `6.22` et `6.27` signifie `6.22 OU 6.27` ;
- ajouter `Usage = Travail` signifie aussi `ET Travail`.

Le bouton `+` de **Vues enregistrées** mémorise la recherche et tous les filtres actifs sur le poste.
La flèche située au bord droit du panneau permet de masquer ou réafficher les filtres ; cet état est conservé au prochain démarrage. Les poignées `⋮⋮` réordonnent manuellement les catégories et les tags.

`Ctrl+K` ouvre la palette rapide. Saisir un nom de carte, de tag ou de mode, naviguer avec `↑`/`↓`, puis valider avec `Entrée`.

## Gérer les tags

Ouvrir `•••` puis **Gérer les tags**. Chaque tag possède :

- une catégorie, par exemple `Usage`, `Version`, `Chants`, `Année` ou `Client` ;
- un nom, par exemple `Travail`, `EP`, `SC`, `2021` ou `6.22` ;
- une couleur.

Dans l'éditeur, la catégorie est une liste modifiable : sélectionner une catégorie existante évite les variantes de saisie, mais il reste possible de saisir directement une nouvelle catégorie.

La suppression d'un tag le retire également de toutes les cartes qui l'utilisent.

## Gérer les ressources partagées

Le bloc **Ressources** du panneau gauche regroupe les dossiers, fichiers et liens utiles à l'équipe. La ressource **Licences TopSolid** est prévue pour le dossier des codes de licence.

Dans `•••` puis **Gérer les ressources**, renseigner autant d'emplacements que nécessaire, par exemple :

- `Local` avec un chemin serveur ou réseau ;
- `Distant · Nextcloud` avec le chemin du dossier synchronisé.

Cliquer sur le bouton principal ouvre le dernier emplacement utilisé sur ce poste. La flèche permet d'en choisir un autre. L'emplacement partagé par défaut et l'ordre des ressources sont modifiables sans limiter leur nombre.

## Partager un catalogue

Dans `•••`, choisir **Choisir un catalogue partagé** :

- **Oui** ouvre un catalogue existant ;
- **Non** crée une copie du catalogue actuel dans l'emplacement choisi.

Un dossier Nextcloud, OneDrive ou réseau peut être utilisé. Les favoris, récents, derniers modes/emplacements et vues enregistrées ne sont pas partagés.

Le pied de la fenêtre indique en permanence si le catalogue est local, partagé ou indisponible, ainsi que son chemin. Cliquer sur cet état permet de changer de catalogue, de le recharger, d'ouvrir son dossier ou de revenir au catalogue local.

Lorsqu'un autre poste enregistre une modification, le catalogue est rechargé automatiquement sous quelques secondes. Si une fenêtre d'édition est ouverte, le rechargement attend sa fermeture. En cas d'indisponibilité temporaire du serveur au démarrage, le chemin partagé est conservé et le launcher peut tenter de s'y reconnecter avec **Recharger maintenant**.

## Sauvegarder ou transférer

- **Exporter le catalogue** crée une copie JSON transportable ;
- **Importer le catalogue** remplace le catalogue courant après confirmation ;
- chaque modification crée automatiquement une sauvegarde dans le dossier `Backups` situé à côté du catalogue ;
- **Historique et sauvegardes** affiche l'auteur, le poste et l'heure des changements récents, puis permet de restaurer une version précédente.

Lors de la première modification avec la version 0.4.0, le catalogue 0.3.2 est sauvegardé avant conversion. Toutes les cartes existantes sont conservées et leur commande devient un mode **Par défaut**.

Le menu **Restaurer une sauvegarde** ouvre directement ce dossier et remplace le catalogue après confirmation, tout en sauvegardant d'abord son état actuel.

## Diagnostic

Le menu **Vérifier le catalogue** contrôle en une fois les modes de lancement, accès rapides et ressources, puis permet d'ouvrir directement l'élément concerné. Le menu **Ouvrir le journal** affiche le dossier contenant les traces de lancement et les erreurs. En cas de cible invalide, la carte reste visible mais son mode est désactivé et la raison est affichée.

Le bloc **Ressources** du panneau gauche donne également accès à la page officielle des téléchargements TopSolid.
