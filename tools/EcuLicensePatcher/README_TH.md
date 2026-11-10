# ECU License Patcher — รุ่นทดลองแก้ปัญหาเปิดไม่ขึ้น

อัปเดต: เปลี่ยน references ที่ inject ให้เป็น **mscorlib / System ของ .NET Framework** แทน .NET 8 เพื่อแก้สาเหตุที่อาจทำให้ EXE ที่ดัดแปลงเปิดไม่ขึ้น พร้อมเพิ่ม diagnostics ลงที่ `%TEMP%\JamesDev-ECU-V2-diagnostic.log`.

## ขั้นตอนบน Windows VM ที่ไม่ต่อรถ
1. ดาวน์โหลด Artifact `ECU-V2-Patcher-Experimental` จาก workflow ล่าสุด (อย่าใช้ Patcher EXE รุ่นเก่า)
2. แตก ZIP และวางไฟล์ต้นฉบับ `ECU-Tuning-Solutions-1.02-community-v2.exe` ไว้ด้วย
3. เปิด CMD ในโฟลเดอร์นั้น แล้วใช้คำสั่ง:

```bat
EcuLicensePatcher.exe "ECU-Tuning-Solutions-1.02-community-v2.exe" "ECU-Tuning-V2-FIXED-TEST.exe"
```

4. วาง `License.Client.exe` และ `public.pem` ที่เป็นคู่ของคีย์ออก License ไว้ในโฟลเดอร์เดียวกับ EXE ตัวใหม่ (ไม่ใส่ private.pem ในโฟลเดอร์ลูกค้า)
5. ตรวจสอบ `License.Client.exe check` ต้องขึ้น Valid
6. ทดสอบเปิด `ECU-Tuning-V2-FIXED-TEST.exe` ภายใน VM เท่านั้น
7. เปิด log โดยคำสั่ง:

```bat
type "%TEMP%\JamesDev-ECU-V2-diagnostic.log"
```

ถ้าไม่มี log หรือหน้าต่างไม่เปิด ให้บันทึกข้อความ Error จาก CMD, ข้อมูล Event Viewer (.NET Runtime / Application Error) และค่า SHA256 ของ original/patched เพื่อวิเคราะห์ต่อ

## ข้อจำกัด
- ยังไม่ผ่านการทดสอบว่าเปิดหน้าต่าง ECU และโหลด embedded payload ได้
- เมธอด ActivationForm แบบ ETS1 ยังคงอยู่ โปรแกรมนี้เป็น proof of concept เท่านั้น ไม่ใช่การย้าย License ทั้งหมด
- ห้ามนำไปเชื่อม ECU จริงก่อนตรวจสอบความปลอดภัยของไฟล์เดิมและทดสอบแยก
- ห้ามอัปโหลด private.pem หรือ token ของลูกค้าขึ้น GitHub
