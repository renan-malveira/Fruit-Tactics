# Fruit Tactics 2.2 — publicar no iOS

App na App Store: **id6761233443**, bundle `com.suricatusgames.fruittactics.mobile`
(desenvolvedor na loja: Automa Trade LTDA). A 2.2 é uma atualização da 1.0, não um app novo.

O que já está pronto no projeto:

| Item | Valor |
|---|---|
| AdMob iOS App ID | `ca-app-pub-8609532543875876~6844724801` (GoogleMobileAdsSettings) |
| Intersticial iOS | `ca-app-pub-8609532543875876/3859468812` (StoreConfig) |
| Banner iOS | `ca-app-pub-8609532543875876/5932852405` (StoreConfig) |
| Firebase iOS | `Assets/GoogleService-Info.plist` (projeto fruit-tactics) |
| Consentimento GDPR | mensagem "Fruit Tactics iOS - GDPR" publicada no AdMob |
| Ícone | `Assets/Sprite/AppIcon_iOS.png`, 1024x1024 sem transparência |
| Info.plist | texto do ATT, `ITSAppUsesNonExemptEncryption = NO`, coleta do Analytics desligada até o consentimento |
| Aparelhos | iPhone + iPad, retrato, tela cheia, iOS 15+ |

---

## 1. Antes do Mac (uma vez)

1. **app-ads.txt no site.** Suba o arquivo `app-ads.txt` da raiz deste repositório para a
   raiz de suricatusgames.com (WordPress > File Organizer, a mesma pasta acima de `jogos/`).
   Confira em https://suricatusgames.com/app-ads.txt: tem que abrir a linha
   `google.com, pub-8609532543875876, DIRECT, f08c47fec0942fa0`.
   Depois, em AdMob > Apps > Fruit Tactics (iOS), clique em **Verificar app**.
   Sem isso o AdMob deixa o iOS em "veiculação limitada".
2. Commit e push de tudo, incluindo `GoogleService-Info.plist` e o `.meta` que o Unity criar.

## 2. No Mac

Precisa ter: Unity **6000.3.5f2** com o módulo *iOS Build Support*, **Xcode 26 ou mais novo**
(a App Store exige o SDK do iOS 26), CocoaPods (`brew install cocoapods`) e uma Apple ID
no time dono do app.

1. `git pull` e abra o projeto no Unity.
2. **File > Build Profiles > iOS > Switch Platform.**
3. **Tools > Release > 1. Aplicar configurações de loja.** Com o iOS ativo, ele também
   coloca os ícones. Tem que aparecer: versão 2.2, bundle `com.suricatusgames.fruittactics.mobile`,
   App ID do AdMob `~6844724801`, iPhone + iPad.
4. **Tools > Release > 3. Validar bloqueadores - iOS.** Esperado: 0 bloqueadores. O único
   aviso deve ser o Team ID (ele é escolhido no Xcode).
5. Em Build Profiles, deixe **Development Build desmarcado** e clique em **Build** (não
   "Build and Run"). Gere numa pasta fora de `Assets`, por exemplo `Builds/iOS`.
   No fim, o Unity roda o `pod install`. Se ele falhar, rode no Terminal:

   ```
   cd Builds/iOS
   pod install --repo-update
   ```

6. Abra **`Unity-iPhone.xcworkspace`** (o `.xcworkspace`, não o `.xcodeproj`).
7. Alvo **Unity-iPhone > Signing & Capabilities**: *Automatically manage signing* ligado e
   **Team** = o time do app. Confira se aparecem *Push Notifications* e
   *Background Modes > Remote notifications*.
8. Aba **General**: Version **2.2**, Build **1**.
9. Destino **Any iOS Device (arm64)** > **Product > Archive** > **Distribute App** >
   **App Store Connect** > **Upload**.

## 3. TestFlight (antes de mandar para revisão)

Instale o build pelo TestFlight num iPhone e, se der, num iPad:

- [ ] Na primeira abertura: termos → (só na Europa) consentimento GDPR → pedido do ATT
      com o texto em português → só depois o pedido de notificações.
- [ ] Login de convidado funciona: o progresso salva, o ranking carrega e
      Configurações > Excluir conta funciona.
- [ ] Banner embaixo do menu; intersticial a partir da fase 4, no máximo a cada 2 partidas
      e 60 s. Unidades novas levam até 1 h para começar a mostrar anúncios.
      **Não clique nos seus próprios anúncios.**
- [ ] O layout fica certo no iPad.
- [ ] Os termos não falam em Android nem Google Play (regra 2.3.10 da Apple).

## 4. App Store Connect (versão 2.2)

1. **+ Versão** → 2.2. Escreva o "Novidades nesta versão" em pt-BR, en e es.
2. Selecione o build que subiu.
3. **Privacidade do app**: revise o que a 1.0 declarou. Com AdMob e Firebase, a 2.2 coleta:
   - *Identificadores*: ID do dispositivo (IDFA), para publicidade de terceiros; marque
     **rastreamento**. ID do usuário (UID do Firebase), para funcionalidade do app.
   - *Dados de uso*: interação com o produto (análises) e dados de publicidade
     (publicidade de terceiros).
   - *Localização aproximada* (pelo IP, usada pelo AdMob), para publicidade de terceiros.
   - *Diagnóstico*: dados de falhas e de desempenho (SDKs do Google).
   - *Conteúdo de jogo*: o nome no ranking e as pontuações, para funcionalidade do app,
     ligados ao usuário.
4. **Classificação etária**: confira se o questionário novo da Apple está respondido.
5. **URL de marketing**: mantenha suricatusgames.com. É lá que o AdMob procura o
   `app-ads.txt` do iOS.
6. Capturas de tela: reaproveite as da 1.0, ou atualize se a interface mudou
   (iPhone 6,9" e iPad 13").
7. **Notas para o revisor**: "O jogo entra como convidado automaticamente; não precisa
   criar conta. O pedido de rastreamento (ATT) serve só para anúncios personalizados."
8. A pergunta de criptografia não aparece, porque o Info.plist já responde.
   **Enviar para revisão.**

## 5. Depois de aprovado

- AdMob > Apps: o iOS sai de "Requer revisão" para "Pronto" depois que o app-ads.txt
  é verificado.
- Notificações push no iOS (opcional): para mandar campanhas pelo Unity Cloud, envie a
  chave APNs (.p8) da Apple em Unity Cloud > Push Notifications > iOS.
- Na próxima build do iOS: aumente o Build (2, 3, ...) ou a versão. A Apple não aceita
  repetir um build.
