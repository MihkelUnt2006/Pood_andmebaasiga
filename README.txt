POOD ANDMEBAASIGA – kuidas käivitada
====================================
Variant A (kõige lihtsam): ava Pood_andmebaasiga.csproj Visual Studios (2019/2022), vajuta F5.
  NuGet taastab PdfSharp automaatselt.

Variant B (oma projekt, nagu juhendis): Create new project -> Windows Forms App (.NET Framework 4.7.2),
  nimi Pood_andmebaasiga. Kustuta Form1.cs, lisa kõik .cs failid (Add -> Existing Item),
  Program.cs asenda minu omaga. References -> Add Reference -> Assemblies -> Microsoft.VisualBasic.
  NuGet: Install-Package PdfSharp -Version 1.50.5147

Andmebaas (Tooded/Kategooriad/Kasutajad/Kliendid/Muugid/MuugiRead) luuakse esimesel käivitamisel
automaatselt kausta %LocalAppData%\Pood_andmebaasiga\Andmed. Pildid -> bin\Debug\Pildid, tšekid (PDF) -> bin\Debug\Arved.

Omaniku registreerimiskood: OMANIK2026 (konstant RegisterForm.cs failis).
Esimesena registreeri Omanik, siis saad lisada tooteid ja kliente.
