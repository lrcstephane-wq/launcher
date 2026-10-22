# Historique des versions

## 0.4.0

- nouvelle identité Idéo Launcher avec fusée dans l'en-tête, la fenêtre et la barre des tâches ;
- modes de lancement illimités par carte, notamment Local et Distant · Nextcloud ;
- bouton scindé lançant le dernier mode utilisé ou affichant tous les modes disponibles ;
- modes et chemins indisponibles signalés sans masquer la configuration ;
- ordre manuel stable des cartes, y compris après un lancement, avec glisser-déposer détaillé et compact ;
- glisser-déposer manuel des catégories de filtres et des tags ;
- ressources partagées à emplacements multiples, avec un accès Licences TopSolid prêt à configurer ;
- icônes ajoutées aux menus principaux et aux menus des cartes ;
- palette de lancement rapide `Ctrl+K`, édition en lot et import groupé de raccourcis `.lnk` ;
- validation globale du catalogue, historique des modifications et restauration des sauvegardes ;
- migration automatique du schéma 2 vers le schéma 4 avec conservation intégrale des cartes existantes et sauvegarde préalable ;
- test de non-régression Windows dédié à la migration 0.3.2 → 0.4.0.

## 0.3.2

- cartes redimensionnées et réorganisées, avec bandeau d'accent et logo plus visibles ;
- accès rapides personnalisables par carte, ouvrables dans l'Explorateur après validation du dossier ;
- prise en charge des chemins réseau, relatifs au catalogue et des variables d'environnement Windows ;
- ajout des dossiers par sélection ou glisser-déposer dans l'éditeur de carte ;
- filtres latéraux et groupes repliables avec mémorisation de leur état ;
- filtre « Favoris uniquement » et recherche étendue aux accès rapides ;
- correction de l'affichage immédiat des vues enregistrées ;
- fenêtres de saisie redimensionnées automatiquement ;
- catégories de tags sélectionnables dans une liste modifiable ;
- date française dynamique et accès direct aux téléchargements TopSolid ;
- ascenseurs, menus contextuels, gouttières et séparateurs harmonisés avec le thème sombre ;
- format du catalogue migré automatiquement en version 2, sans perte des cartes existantes.

## 0.3.1

- thème sombre imposé sur toutes les fenêtres et sur la barre de titre Windows compatible ;
- formulaires de cartes, groupes et tags plus compacts, lisibles et structurés ;
- contrôles, listes de couleurs et messages de validation harmonisés ;
- gestion des tags présentée sous forme de liste compacte ;
- cartes plus grandes et grille responsive selon la largeur disponible ;
- meilleure lisibilité du bandeau, des filtres, des cartes et du pied de fenêtre ;
- état et chemin du catalogue visibles avec accès direct aux commandes principales ;
- rechargement automatique d’un catalogue modifié par un autre poste ;
- conservation du chemin partagé lorsqu’un serveur est temporairement indisponible ;
- retour explicite vers le catalogue local ;
- publication GitHub généralisée à la version déclarée dans le projet.

## 0.3.0

- transformation du lanceur TopSolid en lanceur d'applications générique ;
- nouvelle interface sombre aux couleurs Idéo ;
- cartes, groupes, tags et titres entièrement personnalisables ;
- filtres multi-sélection, recherche et vues enregistrées ;
- favoris, récents, modes compact/détaillé et réorganisation par glisser-déposer ;
- création, modification, duplication et validation des cartes ;
- import de raccourcis Windows `.lnk` et test des commandes ;
- catalogue local ou partagé, sauvegardes automatiques, restauration, import et export ;
- logos copiés à côté du catalogue pour rester portables ;
- détection des modifications concurrentes ;
- redétection des nouvelles installations TopSolid ;
- conservation de la taille, position et maximisation de la fenêtre ;
- documentation d'architecture, d'utilisation, de test et de publication.

## 0.2.0

- mise à jour automatique depuis GitHub Releases ;
- vérification d'intégrité SHA-256 ;
- journal de diagnostic intégré.

## 0.1.0

- détection et lancement des installations TopSolid'Wood V6.
