# ianseo-launcher-exe-dev

> 🇫🇷 Code source du **launcher IANSEO** pour Windows (C# / WinForms) permettant de gérer Apache, MySQL et d’accéder rapidement à IANSEO.  
> Voici le lien vers l'exécutable prêt à l'emploi : [ianseo.exe](https://github.com/Steph-Krs/ianseo-launcher-exe)  
>   
> 🇬🇧 Source code of the **IANSEO launcher** for Windows (C# / WinForms) to manage Apache, MySQL and quickly access IANSEO.  
> Here is the link to the ready-to-use executable: [ianseo.exe](https://github.com/Steph-Krs/ianseo-launcher-exe)  

---

## ✨ Fonctionnalités

- ▶️ Démarrer Apache & MySQL  
- ⏸️ Arrêter Apache & MySQL  
- 🌐 Ouvrir IANSEO dans le navigateur  
- 📸 Générer un QR-Code pour accéder à IANSEO depuis un smartphone  
- 🔗 Copier l’URL d’accès au presse-papier  
- ⚡ Lancer `xampp-control.exe` si nécessaire  
- 🌍 Interface multilingue (FR, EN, ES, DE, IT)

---

## 🛠️ Développement

### Prérequis
- Visual Studio 2022 ou supérieur  
- .NET Framework 4.7.2 (ou version compatible utilisée dans le projet)  
- NuGet (pour restaurer les dépendances)  

### 📥 Installation (dev)
1. Clonez ce dépôt :  
   ```bash
   git clone https://github.com/Steph-Krs/ianseo-launcher-exe-dev.git
   ```
2. Ouvrez la solution `IANSEO_Launcher.sln` dans Visual Studio.  
3. Restaurez les packages NuGet (menu **Tools > NuGet Package Manager > Restore**).  
4. Compilez le projet (`Ctrl+Shift+B`).  
5. L’exécutable sera généré dans le dossier `bin/Release` ou `bin/Debug`.

---

## 📦 Dépendances

- [QRCoder](https://github.com/codebude/QRCoder) (via NuGet)  
- [Resource.Embedder](https://github.com/MarcStan/resource-embedder) (via NuGet)  

Toutes les dépendances sont automatiquement restaurées via NuGet.

---

## 📝 English

### Features
- Start/Stop Apache & MySQL  
- Open IANSEO in browser  
- Generate a QR-Code to access IANSEO from mobile  
- Copy the access URL to clipboard  
- Launch `xampp-control.exe` if needed  
- Multilingual interface (FR, EN, ES, DE, IT)

### Development
1. Clone this repository:  
   ```bash
   git clone https://github.com/Steph-Krs/ianseo-launcher-exe-dev.git
   ```
2. Open `IANSEO_Launcher.sln` in Visual Studio.  
3. Restore NuGet packages.  
4. Build the project.  
5. The `.exe` will be available in `bin/Release` or `bin/Debug`.

### Dependencies
- [QRCoder](https://github.com/codebude/QRCoder) (via NuGet)  
- [Resource.Embedder](https://github.com/MarcStan/resource-embedder) (via NuGet)  
