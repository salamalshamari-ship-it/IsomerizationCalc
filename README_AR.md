# حاسبة وحدة الازمرة — نسخة ويندوز

## 1) تشغيل البرنامج للتجربة
1. فك ضغط المجلد في مكان ثابت (مثلاً `D:\Projects\IsomerizationCalc`).
2. افتح Visual Studio 2026 ← **Open a project or solution** ← اختر الملف `IsomerizationCalc.csproj`.
3. اضغط **F5**. إذا ظهرت نافذة البرنامج فكل شيء تمام.

> إذا ظهر خطأ عن إصدار `.NET`: افتح `IsomerizationCalc.csproj` وغيّر السطر
> `net10.0-windows` إلى `net8.0-windows` (أو الإصدار الذي عندك).
> وإذا ظهر أي خطأ آخر: انسخ نصه وأرسله لي.

## 2) إنشاء ملف التثبيت Setup.exe
1. اضغط مرتين على `publish.bat` (ينتظر دقيقة تقريباً). ينتج مجلد `publish` وفيه `IsomerizationCalc.exe`.
2. افتح `installer.iss` ببرنامج **Inno Setup** واضغط **Ctrl+F9** (Compile).
3. يظهر ملف التثبيت في المجلد `installer_output` باسم `IsomerizationCalc_Setup_1.0.0.exe`.
4. ثبّته على أي كمبيوتر. الحذف يكون من **Settings ← Apps** أو من قائمة Start، ولا يحتاج برنامجاً إضافياً.

ملف سجل الحفظ (CSV) يُحفظ في `Documents\Isomerization Calculation` ولا يُحذف عند إلغاء التثبيت.

## 3) أرقام للمقارنة مع تطبيق الهاتف
هذه النتائج مأخوذة من تشغيل كود `Calc.java` الأصلي. أدخل الأرقام نفسها في البرنامج الجديد وقارن.

**Production** (Feed 2400 / 0.690، Saladin 1500 / 0.700، North Flow 600، L.P.G 150 / 0.560)
- Volumes: Feed 3478، Saladin 2143، North 857، L.P.G 268
- Yield = 86.25%، Total production = 3000
- Hourly: Feed 145، Saladin 89، North 36، Sal + North 125، L.P.G 11

**RON** (iC4 1.2، nC4 2.5، iC5 28.4، nC5 18.3، CP 3.1، 22DMB 9.6، 23DMB 2.2، 2MP 10.1، 3MP 6.5، nC6 4.0، MCP 0.6، Benzene 0.1، CHX 1.0، P7+ 2.4، N7+ 11.0)
- RON = 78.9817، PIN = 97.2335، Σ Area = 98.60، حقل N7+ يعرض 8.6000 بعد الخروج منه

**X-Factor** (CHX 1.2، Benzene 0.4، C7+ 3.1) ← 4.7000 wt%

**LHSV** (Density 0.69، Flow 95، Catalyst 20) ← 6.8841

**H₂/HC**
- Make Up: H₂ 92، C1 4، C2 2، C3 1، iC4 0.5، nC4 0.3، iC5 0.1، nC5 0.1، Flow 1200 kg/hr
- Recycle: H₂ 85، C1 6، C2 4، C3 2، iC4 1، nC4 1، iC5 0.5، nC5 0.5، Flow 18 t/hr
- Feed (22 مكوّناً بترتيب تقرير GC): IC4 0.1، NC4 0.2، IC5 12، NC5 15، 2.2DMB 1، C.P 2، 2.3DMB 1.5، 2.MP 8، 3.MP 5، N.HEX 14، 2.2DMP 0.2، M.C.P 6، 2.4DMP 0.3، BEN 2، C.HEX 3، 2.M-HEX 3، 2.3-DMP 1، 3M-HEX 3، CIS-1.3-DMCP 0.2، TRANS-1.3-DMCP 0.3، N-HEP 20، M.C.H 1.2، Flow 40 t/hr
- Avg M.Wt: Make Up 4.1480، Recycle 6.6447، Feed 84.8414
- Molar: Make Up 289.2974، Recycle 2708.9259، Feed 471.4680
- H₂ Molar: Make Up 266.1536، Recycle 2302.5870
- **H₂/HC = 5.4484**

## 3-ب) القائمة العلوية وقاعدة البيانات
- **File ▸ فتح**: يقبل فقط ملف Excel المُصدَّر من البرنامج (ورقتا Summary وInputs بعناوينهما) ويرفض أي ملف آخر، ويضع أرقام أحدث سجل في أماكنها.
- **File ▸ حفظ** (Ctrl+S): مثل زر «حفظ الملخص». **File ▸ خروج** وزر X: يسأل «هل حفظت عملك؟» (نعم = يخرج، لا = يبقى).
- **Edit**: للخلف، للأمام، قص، نسخ، لصق، اختيار الكل (باختصاراتها المعتادة).
- **About**: الإصدار 1.0 والمطوّر عبدالسلام جاسم.
- قاعدة البيانات: `%LocalAppData%\IsomerizationCalc\records.json`، وملف Excel: `Documents\Isomerization Calculation\IsomerizationData.xlsx` (يُعاد بناؤه بعد كل حفظ/حذف).
- تبويب Summary ▸ «السجلات المحفوظة»: علّم السجلات ثم «حذف المحدد» (يُحذف من القاعدة ومن Excel)، «تحديد الكل»، و«تحميل» لإرجاع سجل إلى الحقول.

## 3-ج) النسخ الاحتياطي والطباعة وتبويب Units
- **Summary ▸ نسخ احتياطي...**: يحفظ كل السجلات في ملف `.isobak`. **استرجاع...**: «نعم» يستبدل كل شيء بالنسخة، «لا» يدمجها مع الموجود، «إلغاء» لا يفعل شيئاً.
- **Summary ▸ طباعة الملخص**: تفتح نافذة الطباعة (اختيار الطابعة وخصائصها وعدد النسخ)، وتطبع النتائج الرئيسية والتفصيلية والمدخلات.
- **Units**: محوّل وحدات (حرارة بما فيها °F، ضغط، تدفق حجمي وكتلي، كثافة وSG وAPI، حجم، كتلة، طول، مساحة، طاقة، قدرة، قيمة حرارية، لزوجة، سرعة، زمن) مع جدول بكل الوحدات للقيمة نفسها.

## 4) تعديل الشكل بالسحب (بدون كود)
1. في Visual Studio افتح من Solution Explorer الملف الخاص بالتبويب (يظهر لك الشكل ويمكنك سحب الحقول):
   - ‏`ProductionView.xaml` ← تبويب Production
   - ‏`RonView.xaml` ← تبويب RON
   - ‏`XFactorView.xaml` ← تبويب X-Factor
   - ‏`LhsvView.xaml` ← تبويب LHSV
   - ‏`H2HcView.xaml` ← تبويب H₂/HC
   - ‏`SummaryView.xaml` ← تبويب Summary
   - ‏`MainWindow.xaml` ← النافذة وشريط التبويبات وزر مسح
2. اضغط على أي عنصر في الشكل ثم غيّر حجمه وخطه ولونه من نافذة **Properties** (الجانب الأيمن)، أو اسحبه بالفأرة.
3. لإضافة عنصر جديد: افتح **Toolbox** واسحب منه (TextBox أو TextBlock...) إلى الشكل. أي TextBox تضيفه يدخل تلقائياً في زر مسح.
4. خطوط وألوان كل البرنامج في مكان واحد: ملف `App.xaml` (أنماط مثل BodyText وBigNumber وCardBorder والألوان في أعلاه).
5. ملفات `ProductionScreen.cs` و`RonScreen.cs` و`XFactorScreen.cs` و`LhsvScreen.cs` و`H2HcScreen.cs` و`SummaryScreen.cs` لم تعد مستخدمة (فارغة). يمكنك حذفها من Solution Explorer.

## ما لم يُنقل في هذه النسخة
- قراءة ورقة المختبر بالكاميرا (OCR).
