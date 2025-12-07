# ianseo-launcher-exe-dev

> 🇫🇷 Code source du **launcher IANSEO** pour Windows (C# / WinForms) - Gestionnaire complet pour Apache, MySQL avec outils de réparation et configuration avancés.  
> Voici le lien vers l'exécutable prêt à l'emploi : [ianseo.exe](https://github.com/Steph-Krs/ianseo-launcher-exe)  
>   
> 🇬🇧 Source code of the **IANSEO launcher** for Windows (C# / WinForms) - Complete manager for Apache, MySQL with repair tools and advanced configuration.  
> Here is the link to the ready-to-use executable: [ianseo.exe](https://github.com/Steph-Krs/ianseo-launcher-exe)  

---

## ✨ Fonctionnalités

### 🎯 Contrôle & Gestion
- ▶️ Démarrer Apache & MySQL (mode XAMPP ou services Windows)
- ⏸️ Arrêter Apache & MySQL intelligemment selon leur mode de lancement
- 🌐 Ouvrir IANSEO dans le navigateur par défaut
- 📊 Surveillance en temps réel du statut d'Apache et MySQL
- 🔄 Détection automatique du mode d'exécution (manuel/auto)

### 🌐 Accès Réseau
- 📸 Générer un QR-Code pour accéder à IANSEO depuis un smartphone
- 🔗 Copier l'URL d'accès au presse-papier
- 🖥️ Détection automatique de l'adresse IP locale et du port Apache

### 🛠️ Outils Avancés
- ⚡ Lancer `xampp-control.exe` directement depuis l'interface
- 🔧 **Réparer MySQL** : restauration automatique depuis backup avec préservation des données utilisateur
- 🚀 **Configurer les services Windows** : installation et activation d'Apache/MySQL comme services au démarrage
- 🛡️ **Configurer Windows Defender** : ajout automatique des exclusions
- 🔐 Gestion des droits administrateur avec relance automatique

### 🌍 Interface
- Interface multilingue (FR, EN, ES, DE, IT)
- Détection automatique de la langue système
- Barre de progression animée avec feedback visuel
- Design moderne avec indicateurs colorés

---

## 🛠️ Développement

### Prérequis
- Visual Studio 2022 ou supérieur  
- .NET Framework 4.7.2 (ou version compatible utilisée dans le projet)  
- NuGet (pour restaurer les dépendances)  

### 📥 Installation (dev)

1. **Clonez** ce dépôt :
   ```bash
   git clone https://github.com/Steph-Krs/ianseo-launcher-exe-dev.git
   cd ianseo-launcher-exe-dev
   ```

2. **Ouvrez** la solution `IANSEO_Launcher.sln` dans Visual Studio

3. **Restaurez** les packages NuGet :
   - Menu **Tools → NuGet Package Manager → Restore NuGet Packages**
   - Ou clic droit sur la solution → **Restore NuGet Packages**

4. **Configurez** la cible de build :
   - Cible : **Release** (pour production) ou **Debug** (pour développement)
   - Plateforme : **Any CPU** ou **x86** selon votre environnement

5. **Compilez** le projet :
   - Appuyez sur `Ctrl+Shift+B`
   - Ou menu **Build → Build Solution**

6. **Récupérez** l'exécutable :
   - Chemin : `bin/Release/IANSEO.exe` ou `bin/Debug/IANSEO.exe`
   - Les dépendances (QRCoder.dll) seront dans le même dossier

---

## 📦 Dépendances

### NuGet Packages
- **[QRCoder](https://github.com/codebude/QRCoder)** `v1.x` - Génération de QR-Codes
- **[Resource.Embedder](https://github.com/MarcStan/resource-embedder)** `v2.x` - Intégration des ressources dans l'exe

Toutes les dépendances sont automatiquement restaurées via NuGet.

### Références Système
- `System.Windows.Forms` - Interface utilisateur
- `System.Drawing` - Gestion des graphiques et QR-Codes
- `System.Management` - Gestion des services Windows
- `System.Diagnostics` - Contrôle des processus

---

## 🔧 Configuration & Personnalisation

### Changer la Langue par Défaut

Éditez la ligne 77 dans `MainForm.cs` :
```csharp
// Automatique (recommandé)
CultureInfo culture = Thread.CurrentThread.CurrentUICulture;

// Forcer une langue
// CultureInfo culture = new CultureInfo("it"); // en, fr, de, es, it
```

### Modifier l'Intervalle de Vérification

Ligne 440 dans `InitTimer()` :
```csharp
checkTimer.Interval = 1000; // en millisecondes (1000 = 1 seconde)
```

### Personnaliser les Noms de Services

Lignes 66-67 dans le constructeur :
```csharp
private string apacheServiceName = "Ianseo_Apache";
private string mysqlServiceName = "Ianseo_MySQL";
```

### Ajouter une Nouvelle Langue

1. **Créez** un nouveau fichier `.resx` dans `Properties/` :
   - Ex: `Resources.pt.resx` pour le portugais
2. **Copiez** toutes les clés depuis `Resources.resx`
3. **Traduisez** les valeurs
4. **Recompilez** le projet

---

## 📝 English

### Features

- Start/Stop Apache & MySQL (XAMPP mode or Windows services)
- Open IANSEO in browser
- Generate QR-Code for mobile access
- Copy access URL to clipboard
- Launch `xampp-control.exe` directly
- **Repair MySQL**: automatic restoration from backup
- **Configure Windows Services**: install Apache/MySQL as startup services
- **Configure Windows Defender**: add automatic exclusions
- Multilingual interface (FR, EN, ES, DE, IT)

### Development

1. Clone the repository:
   ```bash
   git clone https://github.com/Steph-Krs/ianseo-launcher-exe-dev.git
   ```
2. Open `IANSEO_Launcher.sln` in Visual Studio
3. Restore NuGet packages
4. Build the project (`Ctrl+Shift+B`)
5. Executable available in `bin/Release` or `bin/Debug`

### Dependencies

- **[QRCoder](https://github.com/codebude/QRCoder)** - QR-Code generation
- **[Resource.Embedder](https://github.com/MarcStan/resource-embedder)** - Resource embedding

All dependencies are automatically restored via NuGet.

### Dependencies
- [QRCoder](https://github.com/codebude/QRCoder) (via NuGet)  
- [Resource.Embedder](https://github.com/MarcStan/resource-embedder) (via NuGet)  

---

## 📄 License

Ce projet est distribué librement pour faciliter l'usage d'IANSEO par les clubs et bénévoles.  
This project is freely distributed to facilitate IANSEO usage by clubs and volunteers.

---

**Développé pour simplifier la vie des bénévoles gérant les compétitions de tir à l'arc** 🏹
