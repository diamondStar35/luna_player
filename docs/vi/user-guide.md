# Hướng dẫn sử dụng Luna Player

Luna Player là trình phát âm thanh và video thân thiện với bàn phím dành cho Windows. Ứng dụng kết hợp phát phương tiện cục bộ, luồng mạng, phát và tải xuống YouTube, ghi âm âm thanh, quản lý tệp, dấu trang, thông báo giọng nói cùng các phím tắt cục bộ và toàn hệ thống có thể tùy cấu hình.

Hướng dẫn này mô tả phiên bản hiện tại của Luna Player. Tên các menu và phím tắt bên dưới sử dụng giao diện tiếng Việt và cấu hình phím tắt mặc định.

[TOC]

## 1. Trước khi bắt đầu

### Yêu cầu hệ thống

Luna Player yêu cầu Windows 10 phiên bản 1809 64-bit trở lên. Tính năng ghi âm riêng biệt một chương trình yêu cầu Windows 10 phiên bản 2004 trở lên. Các tính năng phát và ghi âm khác vẫn khả dụng trên các phiên bản cũ hơn được hỗ trợ.

### Phiên bản cài đặt và di động (portable)

Các bản phát hành của Luna Player được cung cấp dưới dạng trình cài đặt và tệp nén ZIP di động.

- Để cài đặt Luna Player, hãy chạy trình cài đặt bản phát hành và làm theo các bước hướng dẫn. Trình cài đặt có thể tạo phím tắt trên menu Start và màn hình nền, đồng thời có thể đăng ký các loại phương tiện được hỗ trợ.
- Để sử dụng phiên bản di động, hãy giải nén toàn bộ tệp nén ZIP vào một thư mục và chạy `LunaPlayer.exe`. Hãy giữ tất cả các tệp và thư mục đã giải nén cùng nhau.

Cả hai phiên bản đều dùng chung một thư mục cài đặt người dùng. Trình cập nhật tích hợp sẽ nhận diện phiên bản nào đang chạy để tải về trình cài đặt hoặc tệp nén di động tương ứng.

### Khởi động Luna Player

Bạn có thể khởi động Luna Player trực tiếp, mở một tệp được hỗ trợ từ Windows hoặc truyền tệp và thư mục thông qua dòng lệnh. Luna hoạt động dưới dạng một tiến trình đơn (single instance): mở thêm phương tiện khi ứng dụng đang chạy sẽ gửi phương tiện đó tới cửa sổ hiện có.

Nếu tính năng **Ghi nhớ vị trí tệp lần trước** được bật, việc khởi động Luna mà không chọn tệp khác sẽ khôi phục tệp cục bộ gần nhất và vị trí phát cuối cùng của tệp đó nếu tệp vẫn còn trên đĩa.

## 2. Giao diện và khả năng tiếp cận

### Cửa sổ chính

Cửa sổ chính chứa một thanh menu và năm nút bấm:

- **Trước đó** chuyển đến mục trước đó.
- **Tua lại** lùi lại một khoảng bằng bước tua đã chọn.
- **Phát** hoặc **Tạm dừng** thay đổi trạng thái phát.
- **Tua tới** tiến tới một khoảng bằng bước tua đã chọn.
- **Tiếp theo** chuyển đến mục tiếp theo.

Mọi lệnh cũng đều có thể thực hiện thông qua menu hoặc phím tắt. Những mục không áp dụng được cho phương tiện hiện tại sẽ bị vô hiệu hóa. Ví dụ: thuộc tính tệp chỉ khả dụng cho tệp cục bộ chứ không áp dụng cho luồng mạng, và menu **Tùy chọn video** chỉ được bật khi đang phát video YouTube.

### Điều hướng bàn phím

Sử dụng điều hướng chuẩn của Windows trong toàn bộ ứng dụng:

- Nhấn <kbd>Alt</kbd> để vào thanh menu, sau đó sử dụng các phím mũi tên và phím <kbd>Enter</kbd>.
- Nhấn <kbd>Tab</kbd> và <kbd>Shift</kbd>+<kbd>Tab</kbd> để di chuyển giữa các điều khiển trong hộp thoại.
- Sử dụng các phím mũi tên để di chuyển qua danh sách và các lựa chọn.
- Nhấn <kbd>Enter</kbd> để kích hoạt mục đã chọn khi hộp thoại hỗ trợ.
- Nhấn <kbd>Escape</kbd> để đóng hầu hết các hộp thoại mà không áp dụng thay đổi.
- Nhấn <kbd>F1</kbd> trong cửa sổ chính để mở hướng dẫn này.

Trên một điều khiển trong Tùy chọn (Preferences), <kbd>F1</kbd> có mục đích khác: Luna sẽ đọc trợ giúp theo ngữ cảnh cho điều khiển đó. Nếu cây danh mục đang giữ tiêu điểm, phím sẽ giải thích cách di chuyển giữa các trang cài đặt.

### Đọc thông báo giọng nói

Luna thông báo trạng thái phát, thời gian, âm lượng, điều hướng, thao tác tệp và các thay đổi khác thông qua một trình đọc màn hình được hỗ trợ. Có hai mức độ chi tiết:

- **Người mới bắt đầu** sử dụng các câu thông báo đầy đủ, mang tính giải thích.
- **Nâng cao** sử dụng các xác nhận ngắn gọn hơn.

Nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>V</kbd> để chuyển đổi mức độ chi tiết ngay lập tức, hoặc chọn trong trang **Chung** của Tùy chọn. Bật **Đọc tên tệp khi điều hướng (Trước/Tiếp theo)** nếu bạn muốn Luna đọc tên mỗi mục khi chuyển bài bằng phím Trước đó hoặc Tiếp theo.

### Các nút điều khiển đa phương tiện của Windows

Luna tích hợp với lớp phủ đa phương tiện của Windows và các phím đa phương tiện phần cứng tương thích. Lớp phủ có thể hiển thị tiêu đề và tiến trình thời gian hiện tại. Các nút phát, tạm dừng, trước đó, tiếp theo, tua lại và tua tới sẽ kích hoạt các lệnh tương ứng trong Luna.

## 3. Bắt đầu nhanh

Để phát một tệp cục bộ:

1. Nhấn <kbd>Ctrl</kbd>+<kbd>O</kbd>.
2. Chọn một tệp phương tiện hoặc danh sách phát M3U/M3U8.
3. Nhấn <kbd>Dấu cách</kbd> (Space) để tạm dừng hoặc tiếp tục phát.
4. Nhấn mũi tên <kbd>Trái</kbd> hoặc <kbd>Phải</kbd> để tua.
5. Nhấn mũi tên <kbd>Lên</kbd> hoặc <kbd>Xuống</kbd> để thay đổi âm lượng.

Để phát mọi tệp được hỗ trợ trong một thư mục, hãy nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd> và chọn thư mục đó. Sử dụng <kbd>Tab</kbd> và <kbd>Shift</kbd>+<kbd>Tab</kbd> để di chuyển giữa các mục đã nạp.

Để phát video YouTube, nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd> và nhập địa chỉ video. Để tìm kiếm, nhấn <kbd>Ctrl</kbd>+<kbd>Y</kbd>.

## 4. Mở phương tiện

### Mở tệp

Chọn **Tệp > Mở tệp...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>O</kbd>. Hộp thoại chấp nhận các tệp âm thanh, video, M3U và M3U8 được hỗ trợ.

Cài đặt **Bạn muốn mở gì cùng với các tệp?** kiểm soát điều gì sẽ xảy ra khi một tệp phương tiện được mở từ Windows hoặc từ hộp thoại tệp:

- **Chỉ mở tệp đã chọn** chỉ nạp duy nhất tệp đã chọn.
- **Mở tệp và các tệp trong thư mục chính** nạp các tệp được hỗ trợ trong cùng thư mục và chọn tệp được yêu cầu.
- **Mở tệp cùng các tệp trong thư mục chính và thư mục con** quét thư mục chứa tệp và tất cả các thư mục con bên dưới, nạp mọi tệp phương tiện tìm thấy và chọn tệp được yêu cầu.

Quá trình quét đệ quy có hộp thoại tiến trình có thể hủy và thông báo số tệp phương tiện đã tìm thấy. Các tệp được sắp xếp theo thứ tự sắp xếp tự nhiên của Windows.

Khi nhiều tệp được gửi tới Luna cùng lúc và tùy chọn **Chỉ mở tệp đã chọn** đang được đặt, Luna sẽ nạp tất cả các tệp đã được chọn rõ ràng đó thay vì bỏ qua các tệp còn lại.

### Mở thư mục

Chọn **Tệp > Mở thư mục...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd>.

Với hai chế độ mở tệp đầu tiên, Luna nạp các phương tiện được hỗ trợ trực tiếp bên trong thư mục đã chọn. Với chế độ thư mục con, ứng dụng sẽ quét đệ quy. Các thư mục hệ thống và thư mục không thể truy cập sẽ được bỏ qua trong khi quét đệ quy.

### Mở luồng mạng

Chọn **Tệp > Mở liên kết...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>L</kbd>. Nhập một địa chỉ `http` hoặc `https`.

Sử dụng lệnh này cho các luồng phương tiện trực tiếp và danh sách phát M3U hoặc M3U8 từ xa. Sử dụng **Mở liên kết YouTube...** cho các địa chỉ video và danh sách phát của YouTube.

### Mở danh sách phát

Luna hỗ trợ danh sách phát M3U và M3U8 cục bộ và từ xa. Các mục có đường dẫn tương đối trong danh sách phát cục bộ được phân giải từ thư mục của danh sách phát; các mục tương đối trong danh sách phát mạng được phân giải từ địa chỉ URL của nó.

Một tệp kê khai HLS M3U8 được xử lý như một luồng đơn và chuyển đến công cụ phát. Nó không bị tách thành danh sách các đoạn phương tiện riêng lẻ. Các danh sách phát khác sẽ trở thành danh sách các tệp đang mở chứa các đường dẫn cục bộ và liên kết web có thể phát được.

### Mở phương tiện từ bảng nhớ tạm

Sao chép một tệp hoặc thư mục trong File Explorer, quay lại Luna và nhấn <kbd>Ctrl</kbd>+<kbd>V</kbd>. Luna sẽ mở đường dẫn hợp lệ đầu tiên trên bảng nhớ tạm theo chế độ mở tệp đã cấu hình.

### Liên kết phần mở rộng tệp

Trình cài đặt có thể đăng ký Luna cho các loại tệp được hỗ trợ. Bạn cũng có thể sử dụng **Tùy chọn > Chung > Đăng ký phần mở rộng tệp** hoặc **Hủy đăng ký phần mở rộng tệp**.

Việc đăng ký sẽ thêm Luna vào danh sách ứng dụng của Windows cho các phương tiện được hỗ trợ. Windows vẫn có thể yêu cầu bạn chọn Luna trong **Cài đặt (Settings) > Ứng dụng (Apps) > Ứng dụng mặc định (Default apps)** trước khi trở thành trình phát mặc định.

## 5. Các điều khiển phát

### Phát và tạm dừng

Nhấn <kbd>Dấu cách</kbd> (Space) hoặc <kbd>Enter</kbd>, chọn **Trình phát > Phát/Tạm dừng**, hoặc sử dụng nút Phát/Tạm dừng trên cửa sổ chính.

Nếu một mục đã phát xong hoặc chưa được nạp có thể mở lại, Phát/Tạm dừng sẽ nạp lại mục đó. Ở mức độ chi tiết Người mới bắt đầu, Luna sẽ đọc "Phát" hoặc "Tạm dừng"; mức Nâng cao sẽ tránh các thông báo trạng thái dài này.

### Tua

Nhấn mũi tên <kbd>Trái</kbd> hoặc <kbd>Phải</kbd> để di chuyển theo một bước tua. Bạn có thể tua các khoảng cách lớn hơn mà không cần thay đổi bước tua đã chọn:

- <kbd>Shift</kbd>+<kbd>Trái</kbd> hoặc <kbd>Shift</kbd>+<kbd>Phải</kbd> tua hai bước.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Trái</kbd> hoặc <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Phải</kbd> tua bốn bước.
- <kbd>Home</kbd> chuyển về đầu tệp.
- <kbd>End</kbd> chuyển đến cuối tệp.

Khoảng cách tua mặc định là 5 giây. Hãy chọn một giá trị khác từ **Trình phát > Khoảng cách tua**, hoặc nhấn <kbd>Shift</kbd> cùng với một chữ số:

| Phím tắt | Khoảng cách tua |
| --- | --- |
| <kbd>Shift</kbd>+<kbd>1</kbd> | 1 giây |
| <kbd>Shift</kbd>+<kbd>2</kbd> | 5 giây |
| <kbd>Shift</kbd>+<kbd>3</kbd> | 10 giây |
| <kbd>Shift</kbd>+<kbd>4</kbd> | 20 giây |
| <kbd>Shift</kbd>+<kbd>5</kbd> | 30 giây |
| <kbd>Shift</kbd>+<kbd>6</kbd> | 1 phút |
| <kbd>Shift</kbd>+<kbd>7</kbd> | 2 phút |
| <kbd>Shift</kbd>+<kbd>8</kbd> | 3 phút |
| <kbd>Shift</kbd>+<kbd>9</kbd> | 5 phút |
| <kbd>Shift</kbd>+<kbd>0</kbd> | 10 phút |
| <kbd>Shift</kbd>+<kbd>-</kbd> | Giá trị tùy chỉnh từ Tùy chọn Âm thanh |

Khoảng cách tua đã chọn được lưu lại ngay lập tức. Thay đổi **Giá trị tua tùy chỉnh (giây)** sẽ không tự động áp dụng giá trị đó; hãy chọn giá trị từ menu hoặc bằng <kbd>Shift</kbd>+<kbd>-</kbd> khi bạn muốn sử dụng.

### Chuyển đến thời gian

Chọn **Trình phát > Chuyển đến thời gian...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>G</kbd>. Nhập giờ, phút và giây phù hợp với thời lượng hiện tại. Luna sẽ ngăn vị trí đã chọn vượt quá thời lượng của tệp.

### Nhảy theo phần trăm

Chọn **Trình phát > Nhảy đến phần trăm** hoặc sử dụng các phím tắt phần trăm mặc định:

- <kbd>Ctrl</kbd>+<kbd>1</kbd> đến <kbd>Ctrl</kbd>+<kbd>9</kbd> nhảy tới 10% đến 90%.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>1</kbd> đến <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>9</kbd> nhảy tới 15% đến 95%.
- <kbd>Ctrl</kbd>+<kbd>0</kbd> hoặc <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>0</kbd> nhảy tới 100%.

Việc nhảy theo phần trăm yêu cầu biết trước thời lượng, do đó tính năng này không khả dụng cho một số luồng phát trực tiếp.

### Nhận thông tin phát

Luna có thể đọc các thông tin phát lại mà không cần di chuyển tiêu điểm:

- <kbd>E</kbd> đọc thời gian đã phát.
- <kbd>R</kbd> đọc thời gian còn lại.
- <kbd>T</kbd> đọc tổng thời lượng.
- <kbd>P</kbd> đọc vị trí theo phần trăm.
- <kbd>V</kbd> đọc âm lượng hiện tại.
- <kbd>S</kbd> đọc tốc độ phát hiện tại.
- <kbd>Shift</kbd>+<kbd>P</kbd> đọc điều chỉnh cao độ.
- <kbd>B</kbd> đọc giá trị cân bằng âm thanh trái/phải.

### Tên tệp, đường dẫn và tiêu đề phương tiện

Nhấn phím <kbd>F</kbd> liên tiếp để nhận thông tin chi tiết dần về mục hiện tại:

1. Lần nhấn đầu tiên đọc tên tệp, tên luồng hoặc tiêu đề video YouTube.
2. Lần nhấn thứ hai nhanh chóng sẽ đọc đường dẫn cục bộ đầy đủ hoặc địa chỉ luồng.
3. Lần nhấn thứ ba nhanh chóng sẽ sao chép đường dẫn hoặc địa chỉ đó vào bảng nhớ tạm.

Nếu bạn dừng lại quá một khoảng thời gian ngắn, chuỗi thao tác sẽ bắt đầu lại từ tên hiển thị.

Nhấn <kbd>I</kbd> để đọc tiêu đề được lưu trữ trong siêu dữ liệu của phương tiện. Tiêu đề này tách biệt với tên tệp; một tệp cục bộ có thể không có tiêu đề nhúng hoặc có tiêu đề khác với tên tệp của nó.

### Âm lượng

Nhấn mũi tên <kbd>Lên</kbd> hoặc <kbd>Xuống</kbd> để thay đổi âm lượng theo bước âm lượng đã cấu hình. Bước mặc định là 5 điểm phần trăm.

- <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Lên</kbd> đặt âm lượng ở mức tối đa của Luna.
- <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Xuống</kbd> đặt âm lượng ở mức 5%.
- <kbd>V</kbd> đọc âm lượng hiện tại.

Luna cho phép khuếch đại vượt quá 100%, lên tới 2000%. Mức khuếch đại cao có thể làm tăng tiếng ồn nền và gây méo âm thanh gốc. Tính năng chuẩn hóa động và bộ giới hạn có thể giúp kiềm chế các đỉnh âm thanh, nhưng hãy bắt đầu ở mức vừa phải để bảo vệ thính giác và thiết bị của bạn.

### Tốc độ phát

Nhấn <kbd>Ctrl</kbd>+<kbd>Lên</kbd> hoặc <kbd>Ctrl</kbd>+<kbd>Xuống</kbd> để thay đổi tốc độ phát. Nhấn <kbd>Alt</kbd>+<kbd>Y</kbd> để trở về 1x. Tốc độ được giới hạn trong khoảng từ 0.5x đến 4x, và bước điều chỉnh có thể cấu hình trong trang Tùy chọn Âm thanh.

### Cao độ

Thay đổi cao độ không làm thay đổi tốc độ phát.

- <kbd>Shift</kbd>+<kbd>Lên</kbd> tăng cao độ.
- <kbd>Shift</kbd>+<kbd>Xuống</kbd> giảm cao độ.
- <kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>P</kbd> khôi phục cao độ gốc.
- <kbd>Shift</kbd>+<kbd>P</kbd> đọc mức điều chỉnh hiện tại theo nửa cung.

Cao độ được giới hạn trong khoảng 12 nửa cung cao hơn hoặc thấp hơn mức gốc. Bước điều chỉnh có thể cấu hình từ 0.001 đến 12 nửa cung.

### Cân bằng âm thanh stereo

- <kbd>Ctrl</kbd>+<kbd>Trái</kbd> chuyển âm thanh sang phía bên trái.
- <kbd>Ctrl</kbd>+<kbd>Phải</kbd> chuyển âm thanh sang phía bên phải.
- <kbd>B</kbd> đọc phần trăm cân bằng âm thanh hiện tại.

Mức 0 là ở giữa (cân bằng), giá trị âm là lệch sang trái, và giá trị dương là lệch sang phải. Bước cân bằng có thể cấu hình từ 1 đến 100 điểm phần trăm.

### Chọn thiết bị đầu ra

Chọn **Trình phát > Card âm thanh...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>A</kbd>. Chọn một thiết bị đầu ra âm thanh của Windows và nhấn OK. Thiết bị được chọn sẽ được lưu lại ngay lập tức.

## 6. Làm việc với nhiều tệp

### Trước đó, tiếp theo, đầu tiên và cuối cùng

- <kbd>Shift</kbd>+<kbd>Tab</kbd> hoặc <kbd>Page Up</kbd> phát mục trước đó.
- <kbd>Tab</kbd> hoặc <kbd>Page Down</kbd> phát mục tiếp theo.
- <kbd>Ctrl</kbd>+<kbd>Home</kbd> phát mục đầu tiên.
- <kbd>Ctrl</kbd>+<kbd>End</kbd> phát mục cuối cùng.

Nếu tùy chọn **Quay lại đầu danh sách khi có nhiều tệp** được bật, việc nhấn Tiếp theo ở mục cuối cùng sẽ quay lại mục đầu tiên, và nhấn Trước đó ở mục đầu tiên sẽ quay lại mục cuối cùng. Tự động chuyển bài cũng tuân theo quy tắc tương tự.

### Chuyển đến số thứ tự tệp

Chọn **Trình phát > Chuyển đến tệp...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>G</kbd>. Nhập một số từ 1 đến tổng số mục đã nạp.

### Hộp thoại các tệp đang mở

Chọn **Tệp > Các tệp đang mở...** hoặc nhấn <kbd>F2</kbd>. Mục hiện tại sẽ được chọn sẵn trong danh sách.

- Chọn một mục và kích hoạt **Nhảy đến mục đã chọn**, hoặc nhấn <kbd>Enter</kbd> trên mục đó để phát.
- Kích hoạt **Thông tin danh sách phát** để tính toán số lượng tệp, tổng kích thước, tổng thời lượng, thời gian đã phát và thời gian còn lại của toàn bộ danh sách phát. Quá trình tính toán có thể hủy bỏ và kết quả sẽ hiển thị trong một cửa sổ văn bản chỉ đọc.

Danh sách được thiết kế để duy trì tốc độ phản hồi nhanh ngay cả khi có số lượng tệp nạp vào rất lớn.

### Phát ngẫu nhiên

Chọn **Trình phát > Phát ngẫu nhiên** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Z</kbd>. Bật phát ngẫu nhiên sẽ tạo một thứ tự phát ngẫu nhiên trong khi vẫn giữ mục hiện tại được chọn.

Trong khi chế độ ngẫu nhiên đang bật, các thao tác Trước đó, Tiếp theo, Tệp đầu tiên, Tệp cuối cùng, Chuyển đến tệp và hộp thoại Các tệp đang mở đều hoạt động theo thứ tự ngẫu nhiên. Tắt phát ngẫu nhiên sẽ khôi phục lại thứ tự danh sách thông thường trong khi vẫn giữ nguyên mục đang phát.

### Lặp lại và hành vi khi kết thúc tệp

Chọn **Trình phát > Lặp lại tệp** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>R</kbd> để lặp lại mục hiện tại. Nút chuyển đổi khi đang chạy này được ưu tiên áp dụng khi được bật.

Trang Tùy chọn Âm thanh cũng kiểm soát hành vi thông thường khi phát hết một tệp:

- **Chuyển sang tệp tiếp theo** bắt đầu phát mục tiếp theo.
- **Lặp lại tệp** phát lại chính mục đó.
- **Không làm gì cả** dừng phát khi đến cuối tệp.

### Ghi nhớ vị trí

Hai cài đặt này phục vụ các mục đích khác nhau:

- **Ghi nhớ vị trí tệp lần trước** khôi phục tệp cục bộ đang hoạt động và thời gian phát trong lần tiếp theo Luna khởi động.
- **Lưu vị trí hiện tại cho từng tệp** ghi nhớ vị trí riêng biệt cho từng mục cục bộ và quay lại vị trí đó khi mục được mở lại.

Nếu cả hai đều tắt, các tệp thường sẽ bắt đầu phát từ đầu.

## 7. Vòng lặp A-B

Vòng lặp A-B giúp lặp lại một đoạn nhất định của mục hiện tại.

1. Tua đến điểm bắt đầu mong muốn và nhấn <kbd>[</kbd>. Thao tác này đặt điểm A.
2. Tua đến vị trí sau đó và nhấn <kbd>]</kbd>. Thao tác này đặt điểm B và bắt đầu lặp từ A đến B.
3. Nhấn <kbd>Backspace</kbd> để xóa vòng lặp và tiếp tục phát từ điểm B.

Trong khi vòng lặp đang hoạt động, việc tua thông thường chỉ giới hạn trong phạm vi đã chọn. <kbd>Home</kbd> chuyển đến điểm A và <kbd>End</kbd> chuyển đến điểm B. Điểm B bắt buộc phải sau điểm A.

Vùng chọn áp dụng cho mục hiện tại và sẽ tự động xóa khi phát sang một mục khác.

## 8. Loại bỏ khoảng lặng và xử lý âm thanh

### Bật hoặc tắt tính năng loại bỏ khoảng lặng

Chọn **Trình phát > Bật bộ lọc loại bỏ khoảng lặng** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>M</kbd>. Luna lưu trạng thái bật/tắt này ngay lập tức.

Loại bỏ khoảng lặng sẽ rút ngắn các đoạn yên lặng trong khi phát phương tiện. Thao tác này không làm thay đổi tệp gốc. Kết quả phụ thuộc nhiều vào nguồn âm thanh: đặt ngưỡng quá mạnh có thể coi cả giọng nói nhỏ hoặc âm nhạc là khoảng lặng.

### Cài đặt khoảng lặng cơ bản

Mở **Tùy chọn > Loại bỏ khoảng lặng**.

- **Thời lượng khoảng lặng tối thiểu (giây)** kiểm soát độ dài tối thiểu của một đoạn yên tĩnh trước khi nó bị rút ngắn. Tăng giá trị này sẽ giữ lại nhiều khoảng dừng ngắn hơn.
- **Ngưỡng khoảng lặng** là mức âm lượng, tính bằng decibel, mà dưới mức đó âm thanh được coi là khoảng lặng. Giá trị ít âm hơn (như -20) sẽ coi âm thanh lớn hơn là khoảng lặng; giá trị âm hơn (như -50) sẽ giới hạn việc loại bỏ ở âm thanh rất nhỏ.

Các giá trị mặc định là 0.5 giây và -30 dB.

### Cài đặt khoảng lặng nâng cao

Bật **Hiển thị cài đặt nâng cao** để định cấu hình bộ lọc loại bỏ khoảng lặng bên dưới một cách chính xác hơn:

- **Số phần khoảng lặng đầu cần cắt**: 0 để giữ nguyên phần đầu; 1 để loại bỏ khoảng lặng ban đầu cho đến khi có âm thanh liên tục. Giá trị cao hơn tiếp tục cắt qua nhiều đoạn âm thanh hơn.
- **Thời lượng âm thanh yêu cầu trước khi dừng cắt khoảng lặng đầu**: thời gian âm thanh phải duy trì trên ngưỡng trước khi Luna giữ lại.
- **Số phần khoảng lặng cần cắt sau khi âm thanh bắt đầu**: -1 để rút ngắn mọi khoảng dừng đủ điều kiện sau đó, 0 để giữ nguyên khoảng lặng sau này, và một số dương để giới hạn số lượng đoạn được rút ngắn.
- **Độ dài khoảng lặng bên trong tối thiểu**: khoảng dừng tối thiểu đủ điều kiện sau khi âm thanh đã bắt đầu.
- **Khoảng dừng giữ lại sau khi cắt khoảng lặng**: giữ lại một phần của mỗi khoảng dừng để các từ ngữ không bị dính vào nhau.
- **Kích thước cửa sổ phát hiện**: khoảng thời gian được sử dụng cho mỗi lần đo âm lượng. Cửa sổ lớn hơn cho kết quả ổn định hơn; cửa sổ nhỏ hơn phản ứng nhanh hơn.
- **Chế độ phát hiện**: Peak phản ứng với mẫu âm thanh lớn nhất và âm thanh ngắn; RMS sử dụng năng lượng trung bình để phát hiện mượt mà hơn.

Hãy thực hiện những thay đổi nhỏ và kiểm thử trên các đoạn tài liệu đại diện. Bộ lọc sẽ áp dụng cho việc phát lại ngay sau khi bạn nhấn OK trong Tùy chọn.

### Chuẩn hóa, giới hạn và mono

Trang Tùy chọn Âm thanh chứa hai bộ lọc bổ sung:

- **Bật bộ giới hạn và chuẩn hóa động** cân bằng các mức âm lượng thay đổi và kiềm chế các đỉnh âm thanh để giảm méo tiếng.
- **Phát âm thanh dạng Mono** kết hợp kênh trái và kênh phải thành một đầu ra mono duy nhất.

Cả hai tùy chọn đều không làm thay đổi tệp nguồn.

## 9. Quản lý tệp cục bộ

Các lệnh đổi tên, xóa, mở thư mục chứa và thuộc tính Windows chỉ hoạt động với các tệp cục bộ. Chúng không khả dụng cho các luồng mạng và phương tiện YouTube.

### Hiển thị tệp trong Windows

- Nhấn <kbd>Ctrl</kbd>+<kbd>F</kbd> hoặc chọn **Tệp > Mở thư mục chứa** để mở File Explorer với tệp hiện tại được chọn sẵn.
- Nhấn <kbd>Alt</kbd>+<kbd>Enter</kbd> hoặc chọn **Tệp > Thuộc tính tệp...** để mở hộp thoại thuộc tính của Windows.

### Đổi tên tệp

Chọn **Chỉnh sửa > Đổi tên...** hoặc nhấn <kbd>Shift</kbd>+<kbd>F2</kbd>. Nhập tên tệp mới. Nếu bạn bỏ qua phần mở rộng, Luna sẽ giữ nguyên phần mở rộng hiện tại. Luna sẽ từ chối nếu tên để trống hoặc tên đó đã được sử dụng trong thư mục.

### Xóa tệp

Chọn **Chỉnh sửa > Xóa** hoặc nhấn <kbd>Shift</kbd>+<kbd>Delete</kbd>. Xác nhận lời nhắc để xóa vĩnh viễn tệp hiện tại khỏi đĩa và xóa tệp đó khỏi danh sách đã nạp. Luna không chuyển tệp vào Thùng rác (Recycle Bin).

Xóa tệp không giống với lệnh **Đóng tệp**. Đóng tệp chỉ gỡ mục đó khỏi Luna; xóa tệp sẽ xóa hẳn tệp cục bộ khỏi đĩa cứng.

### Sao chép tệp

Nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd> để đặt mục hiện tại vào bảng nhớ tạm của Windows. Sau đó bạn có thể dán vào File Explorer hoặc chương trình khác chấp nhận sao chép tệp.

### Đóng tệp

- Nhấn <kbd>Ctrl</kbd>+<kbd>W</kbd> để gỡ mục hiện tại khỏi Luna mà không xóa tệp.
- Nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>W</kbd> để gỡ bỏ tất cả các mục đã nạp.

## 10. Các tệp đã đánh dấu và thao tác hàng loạt

Tính năng đánh dấu cho phép bạn tập hợp các tệp cục bộ trong lúc điều hướng và sau đó thao tác trên tất cả các tệp đó cùng một lúc.

- Nhấn <kbd>Ctrl</kbd>+<kbd>K</kbd> để đánh dấu hoặc bỏ đánh dấu tệp hiện tại.
- Nhấn <kbd>Ctrl</kbd>+<kbd>A</kbd> để đánh dấu tất cả các tệp đã nạp hoặc bỏ đánh dấu tất cả nếu tất cả đang được đánh dấu.
- Nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>K</kbd> để bỏ đánh dấu tất cả các tệp.
- Nhấn <kbd>K</kbd> để nghe số lượng tệp đã được đánh dấu.

Khi có ít nhất một tệp được đánh dấu, menu **Thao tác cho các tệp đã đánh dấu** sẽ khả dụng:

- **Sao chép vào thư mục...** sao chép tất cả các tệp đã đánh dấu vào một thư mục được chọn.
- **Di chuyển vào thư mục...** di chuyển các tệp và gỡ bỏ các mục đã di chuyển thành công khỏi Luna.
- **Sao chép vào bảng nhớ tạm** đặt các tệp vào bảng nhớ tạm của Windows.
- **Xóa** yêu cầu xác nhận, xóa vĩnh viễn các tệp khỏi đĩa (không qua Thùng rác) và gỡ bỏ các mục thành công khỏi Luna.

Thao tác sao chép và di chuyển sử dụng hộp thoại tiến trình có thể hủy. Luna sẽ thông báo thành công hoàn toàn, thành công một phần, bị hủy hoặc thất bại. Nếu một số tệp bị lỗi, những tệp đã xử lý thành công trước đó vẫn được giữ nguyên trạng thái.

## 11. Dấu trang

Dấu trang giúp lưu các vị trí phát kèm theo tên cho các tệp cục bộ. Chúng được lưu trữ theo từng tệp và không khả dụng cho các luồng mạng hoặc video YouTube.

### Thêm dấu trang

1. Phát hoặc tua đến vị trí mong muốn.
2. Chọn **Dấu trang > Thêm dấu trang mới** hoặc nhấn <kbd>Shift</kbd>+<kbd>M</kbd>.
3. Chấp nhận tên dựa trên thời gian được tạo tự động hoặc nhập một tên mang tính gợi nhớ.

Các dấu trang được sắp xếp theo vị trí phát. Mười dấu trang đầu tiên theo thứ tự đó sẽ chiếm các vị trí phím tắt trực tiếp.

### Nhảy đến dấu trang theo số

Nhấn <kbd>Alt</kbd>+<kbd>1</kbd> đến <kbd>Alt</kbd>+<kbd>9</kbd> cho các vị trí từ 1 đến 9. Nhấn <kbd>Alt</kbd>+<kbd>0</kbd> cho vị trí thứ 10. Luna sẽ thông báo bằng giọng đọc nếu vị trí được yêu cầu đang trống.

### Quản lý dấu trang

Chọn **Dấu trang > Quản lý dấu trang** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>M</kbd>. Hộp thoại liệt kê tên và vị trí dấu trang của tệp hiện tại.

- **Nhảy đến** chuyển vị trí phát đến dấu trang đã chọn.
- **Chỉnh sửa** thay đổi tên của dấu trang.
- **Xóa** gỡ bỏ dấu trang sau khi xác nhận.
- Nhấn kích hoạt một dấu trang trong danh sách cũng sẽ nhảy trực tiếp đến vị trí đó.

Sử dụng trang Tùy chọn Sao lưu và khôi phục để xuất hoặc nhập toàn bộ bộ sưu tập dấu trang.

## 12. YouTube

Các tính năng YouTube yêu cầu kết nối Internet. Tính khả dụng có thể thay đổi khi YouTube điều chỉnh dịch vụ của họ. Luna bao gồm một bộ phân giải tích hợp và tùy chọn có thể sử dụng yt-dlp.

### Mở liên kết YouTube

Chọn **Tệp > Mở liên kết YouTube...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd>. Nhập địa chỉ video hoặc danh sách phát. Lệnh này không chấp nhận địa chỉ kênh.

Một số địa chỉ YouTube xác định đồng thời cả video và danh sách phát. Luna có thể hỏi bạn muốn phát video hay mở danh sách phát, hoặc tuân theo hành vi cố định được chọn trong Tùy chọn YouTube.

### Tìm kiếm trên YouTube

Chọn **Tệp > Tìm kiếm trên YouTube...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Y</kbd>. Nhập nội dung tìm kiếm, sau đó di chuyển qua danh sách kết quả.

- Nhấn <kbd>Enter</kbd> trên kết quả đã chọn hoặc kích hoạt **Phát** để phát mục đó.
- Kích hoạt **Tải xuống** để lưu về máy.
- Nhấn <kbd>Ctrl</kbd>+<kbd>C</kbd> để sao chép liên kết của kết quả đã chọn.
- Nhấn <kbd>Ctrl</kbd>+<kbd>B</kbd> để mở kết quả trong trình duyệt mặc định.
- Nhấn <kbd>Ctrl</kbd>+<kbd>N</kbd> để điều hướng tới các kết quả kênh của người đăng tải.
- Mở menu ngữ cảnh để có các thao tác sao chép, trình duyệt, kênh và tải xuống tương tự.

Khi vùng chọn đến cuối trang kết quả hiện tại, Luna sẽ tự động yêu cầu thêm kết quả cho đến khi YouTube không còn kết quả nào để trả về.

Các cửa sổ kết quả danh sách phát và kênh hoạt động theo cách tương tự. Phím Tiếp theo sẽ di chuyển tiếp trong phiên kết quả YouTube hiện tại, tự động tải video tiếp theo khi cần. Nhấn <kbd>Escape</kbd> trong cửa sổ chính sau khi bắt đầu phát có thể quay lại danh sách kết quả liên quan nếu phiên đó vẫn còn.

### Chỉ phát âm thanh và chất lượng

Mở **Tùy chọn > YouTube**.

- **Chỉ phát âm thanh của video** chỉ yêu cầu luồng âm thanh, giúp tiết kiệm băng thông và thường phát nhanh hơn.
- **Chất lượng video** chọn Thấp, Trung bình hoặc Tốt nhất khi bật phát hình ảnh.

Các lựa chọn tương tự cũng được áp dụng khi tải xuống video. Chế độ chỉ phát âm thanh sẽ tải âm thanh mà không có hình ảnh; nếu không, Luna sẽ tải xuống theo chất lượng video đã chọn.

### Tùy chọn video

Khi một video YouTube đang phát, menu **Tùy chọn video** cung cấp:

- **Tải xuống...** hoặc <kbd>Ctrl</kbd>+<kbd>D</kbd>: chọn thư mục và lưu video hiện tại kèm tiến trình và khả năng hủy.
- **Mô tả video...** hoặc <kbd>Alt</kbd>+<kbd>D</kbd>: lấy tiêu đề và phần mô tả của người đăng tải và hiển thị trong cửa sổ văn bản chỉ đọc.
- **Sao chép liên kết video**: sao chép địa chỉ xem YouTube ổn định thay vì địa chỉ luồng tạm thời.

Việc tải xuống lặp lại không ghi đè lên tệp hiện có có cùng tên; Luna sẽ tự động đặt tên mới có đánh số thứ tự chưa sử dụng.

### Video và luồng yêu thích

Mục yêu thích lưu các liên kết mạng, trong khi dấu trang lưu các vị trí bên trong tệp cục bộ. Mở danh sách yêu thích bằng **Tệp > Video yêu thích...** hoặc <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd>.

Hộp thoại Video yêu thích cho phép mở, thêm, chỉnh sửa hoặc xóa các mục. Mỗi mục có tên, liên kết và loại:

- **Video** yêu cầu liên kết video YouTube.
- **Danh sách phát** yêu cầu liên kết danh sách phát YouTube.
- **Liên kết kết hợp** yêu cầu địa chỉ chứa cả mã định danh video và danh sách phát, và sẽ mở video của nó.
- **Luồng chung** chấp nhận bất kỳ địa chỉ luồng `http` hoặc `https` nào.

Luna xác thực định dạng địa chỉ khi mục được lưu. Ứng dụng chỉ kiểm tra xem nội dung từ xa có còn khả dụng hay không khi bạn mở nó.

### Bộ phân giải tích hợp và yt-dlp

Luna thường sử dụng bộ phân giải YouTube tích hợp sẵn. Nếu bộ phân giải đó không thể mở được video, hãy bật **Sử dụng yt-dlp để phân giải luồng** trong trang Tùy chọn YouTube. Luna sẽ đề xuất tải xuống thành phần cần thiết nếu bị thiếu.

yt-dlp luôn cần thiết cho nhánh phân giải yt-dlp, nhưng bộ phân giải tích hợp và các tính năng YouTube khác có thể hoạt động mà không cần nó. Trang thành phần cung cấp ba kênh cập nhật:

- **Ổn định** (Stable) ít thay đổi nhất và được khuyến nghị sử dụng thông thường.
- **Nightly** theo sát các bản dựng hằng đêm của yt-dlp.
- **Master** theo sát mã nguồn mới nhất và có thể kém ổn định hơn.

Sử dụng **Tải xuống thành phần YouTube** trong Tùy chọn để cài đặt các tệp thực thi yt-dlp và Deno còn thiếu. Sử dụng **Trợ giúp > Cập nhật > Cập nhật thành phần YouTube** để cập nhật yt-dlp đã cài đặt. Bật **Kiểm tra bản cập nhật yt-dlp khi khởi động** để kiểm tra ngầm trong nền và chỉ thông báo khi có bản cập nhật sẵn sàng.

## 13. Ghi âm âm thanh

Luna có thể ghi âm micrô và các đầu vào khác, toàn bộ âm thanh phát qua thiết bị đầu ra hoặc âm thanh của một chương trình cụ thể. Nhiều nguồn có thể được trộn vào một bản ghi âm duy nhất với các mức âm lượng riêng biệt.

### Ghi âm nhanh với micrô mặc định

Nếu chưa có nguồn nào được tạo trong phiên hiện tại, hãy nhấn <kbd>F9</kbd> để ghi âm thiết bị đầu vào mặc định của Windows bằng các Tùy chọn Ghi âm đã lưu. Nhấn <kbd>F7</kbd> để tạm dừng hoặc tiếp tục, và <kbd>F8</kbd> để dừng.

Một âm báo tăng dần xác nhận quá trình ghi âm bắt đầu. Một âm báo giảm dần xác nhận quá trình ghi âm đã dừng. Việc bắt đầu và dừng thành công sẽ không phát thông báo giọng nói đè lên các âm báo này.

### Mở giao diện ghi âm

Chọn **Ghi âm > Mở giao diện ghi âm...** hoặc nhấn <kbd>Alt</kbd>+<kbd>R</kbd>. Cửa sổ này có danh sách nguồn, các điều khiển nguồn, cài đặt đầu ra và điều khiển Bắt đầu/Tạm dừng.

Việc đóng cửa sổ này không làm dừng quá trình ghi âm đang diễn ra. Phiên ghi âm thuộc về toàn bộ ứng dụng và tiếp tục cho đến khi bạn nhấn Dừng, <kbd>F8</kbd> hoặc thoát Luna.

### Thêm các nguồn

Kích hoạt **Thêm** và chọn:

- **Thiết bị đầu vào...** cho micrô, đầu vào line-in hoặc thiết bị thu âm khác của Windows.
- **Đầu ra hệ thống...** cho tất cả âm thanh phát qua loa hoặc tai nghe đã chọn.
- **Chương trình...** cho âm thanh của riêng một chương trình. Yêu cầu Windows 10 phiên bản 2004 trở lên.

Đặt tên gợi nhớ cho nguồn, chọn thiết bị hoặc chương trình và điều chỉnh âm lượng. Nguồn thiết bị có thể tự động theo thiết bị mặc định hiện tại của Windows thay vì cố định một thiết bị.

Đối với nguồn chương trình, tùy chọn **Ghi âm tất cả ngoại trừ ứng dụng này** sẽ đảo ngược lựa chọn và thu âm thanh của các ứng dụng khác. Chỉ các chương trình có phiên âm thanh Windows đang hoạt động mới xuất hiện trong danh sách; hãy bật âm thanh trong chương trình mục tiêu nếu chưa thấy xuất hiện.

Sử dụng **Chỉnh sửa...** để thay đổi nguồn đã chọn và **Xóa...** để gỡ nguồn khỏi phiên. Thanh trượt âm lượng nguồn sẽ thay đổi mức âm lượng của nguồn đó trong bản phối cuối cùng.

Các nguồn chỉ tồn tại theo phiên. Chúng vẫn được giữ lại khi đóng và mở lại cửa sổ ghi âm trong cùng một phiên chạy Luna, nhưng sẽ không được khôi phục sau khi thoát ứng dụng vì thiết bị và mã định danh tiến trình có thể không còn đại diện cho cùng một nguồn.

### Định dạng và thư mục lưu bản ghi âm

Chọn định dạng, tần số lấy mẫu, số kênh, chất lượng (nếu áp dụng) và thư mục lưu:

- **WAV** không nén và thường tạo ra tệp lớn nhất.
- **FLAC** nén không mất dữ liệu và thường nhỏ hơn WAV.
- **MP3** và **AAC** nén mất dữ liệu và sử dụng tốc độ bit đã chọn để cân đối giữa kích thước và chất lượng.
- **Mono** thường là đủ cho một micrô hoặc giọng nói đơn.
- **Stereo** lưu giữ các kênh trái và phải riêng biệt cho âm nhạc và đầu ra hệ thống.

Các tần số lấy mẫu, số kênh và tốc độ bit khả dụng được lấy từ các bộ mã hóa cài đặt trên Windows. Do đó, các lựa chọn sẽ thay đổi theo định dạng đã chọn và có thể khác nhau tùy hệ thống. Luna tự động đặt tên cho mỗi bản ghi âm có mốc thời gian để tránh ghi đè tệp cũ.

Những thay đổi được thực hiện trong giao diện ghi âm chỉ áp dụng cho phiên làm việc đó. Để thay đổi giá trị mặc định cho những lần khởi chạy sau hoặc cho các phím tắt trực tiếp, hãy sử dụng **Tùy chọn > Ghi âm**.

### Bắt đầu, tạm dừng và dừng

Cửa sổ ghi âm yêu cầu ít nhất một nguồn đã được cấu hình trước khi nút **Bắt đầu** có thể bấm được. Khi quá trình ghi âm bắt đầu, các điều khiển nguồn và đầu ra sẽ bị khóa để đảm bảo định dạng và bản phối ghi âm đồng nhất.

- Nút **Bắt đầu** trở thành nút **Dừng** trong khi đang ghi âm.
- Nút **Tạm dừng** trở thành nút **Tiếp tục** khi đang tạm dừng.
- <kbd>F9</kbd>, <kbd>F7</kbd> và <kbd>F8</kbd> hoạt động từ cửa sổ chính của Luna.
- Các phím tắt toàn cục tương ứng hoạt động ngay cả khi một ứng dụng khác đang có tiêu điểm.

Nếu chỉ một số nguồn có thể mở được, Luna sẽ bắt đầu ghi âm với các nguồn khả dụng và liệt kê những nguồn bị lỗi. Nếu không có nguồn nào mở được, quá trình ghi âm sẽ không bắt đầu.

Chọn **Ghi âm > Mở thư mục bản ghi âm** để mở thư mục lưu trữ hiện tại. Thư mục mặc định là `Documents\Luna Player\Recordings`.

## 14. Tham chiếu cài đặt tùy chọn

Mở cài đặt Tùy chọn bằng **Tệp > Tùy chọn...** hoặc <kbd>Ctrl</kbd>+<kbd>P</kbd>. Chọn một trang trong cây danh mục. Nhấn OK để xác thực và áp dụng tất cả các trang, hoặc nhấn Hủy để hủy bỏ những thay đổi chưa chấp nhận.

Nhấn <kbd>F1</kbd> trong khi một mục cài đặt đang giữ tiêu điểm để nghe giải thích chi tiết về mục cài đặt đó.

### Chung

| Cài đặt | Mục đích |
| --- | --- |
| Ngôn ngữ | Sử dụng ngôn ngữ hiển thị của Windows hoặc một bản dịch đi kèm. Khởi động lại Luna sau khi thay đổi. |
| Ghi nhớ vị trí tệp lần trước | Khôi phục tệp cục bộ hoạt động gần nhất và vị trí phát của nó trong lần khởi chạy tiếp theo. |
| Đọc tên tệp khi điều hướng | Đọc thông báo tên mục khi bạn dùng phím Trước đó hoặc Tiếp theo. |
| Kiểm tra bản cập nhật ứng dụng khi khởi động | Kiểm tra ngầm sau khi khởi động và chỉ hiển thị lời nhắc khi có bản phát hành mới hơn sẵn sàng. |
| Lưu cài đặt khi đóng | Lưu các thay đổi trong phiên như âm lượng và tốc độ khi thoát Luna. Những thay đổi trong Tùy chọn được chấp nhận rõ ràng vẫn được lưu ngay lập tức. |
| Mức độ chi tiết | Chọn thông báo đầy đủ ở mức Người mới bắt đầu hoặc thông báo ngắn gọn ở mức Nâng cao. |
| Bạn muốn mở gì cùng với các tệp? | Kiểm soát việc một tệp được mở sẽ không nạp thêm tệp khác, nạp cùng thư mục hay nạp cả thư mục và các thư mục con. |
| Đăng ký/Hủy đăng ký phần mở rộng tệp | Thêm hoặc gỡ bỏ đăng ký phương tiện được hỗ trợ của Luna cho người dùng Windows hiện tại. |

### Sao lưu và khôi phục

| Lệnh | Mục đích |
| --- | --- |
| Xuất cài đặt | Lưu các tùy chọn đang áp dụng vào một tệp JSON mà không di chuyển bản sao đang hoạt động. |
| Nhập cài đặt | Nạp tệp cài đặt JSON hợp lệ và thay thế các giá trị hiển thị trong Tùy chọn. |
| Xuất dấu trang | Lưu tất cả các dấu trang vào một tệp JSON. |
| Nhập dấu trang | Thay thế bộ sưu tập dấu trang hiện tại bằng một bộ sưu tập đã xuất hợp lệ. |
| Đặt lại cài đặt | Khôi phục mọi tùy chọn và phím tắt về mặc định ban đầu sau khi xác nhận. |
| Mở thư mục cài đặt người dùng | Mở thư mục cấu hình của Luna trong File Explorer. |

Thao tác nhập sẽ thay thế toàn bộ bộ sưu tập tương ứng; nó không hợp nhất các mục. Hãy lưu các tệp đã xuất ở một nơi riêng biệt ngoài thư mục cấu hình của Luna nếu bạn muốn dùng làm bản sao lưu an toàn.

### Âm thanh

| Cài đặt | Mục đích |
| --- | --- |
| Giá trị tua tùy chỉnh | Số giây được sử dụng bởi khoảng cách tua Tùy chỉnh; chấp nhận số thập phân dương như 2.5. |
| Bước tốc độ | Mức tăng hoặc giảm cho mỗi lệnh điều chỉnh tốc độ. |
| Bước cao độ | Mức thay đổi cao độ cho mỗi lần nhấn, từ 0.001 đến 12 nửa cung. |
| Bước âm lượng | Mức thay đổi âm lượng cho mỗi lần nhấn, từ 1 đến 20 điểm phần trăm. |
| Bước cân bằng âm thanh | Mức di chuyển trái/phải cho mỗi lần nhấn, từ 1 đến 100 điểm phần trăm. |
| Làm gì sau khi kết thúc tệp? | Chuyển tiếp sang tệp sau, lặp lại tệp hoặc dừng lại ở cuối tệp. |
| Quay lại đầu danh sách khi có nhiều tệp | Kết nối phần cuối và phần đầu của danh sách có nhiều mục. |
| Lưu vị trí hiện tại cho từng tệp | Duy trì vị trí tiếp tục riêng biệt cho từng mục cục bộ. |
| Bật bộ giới hạn và chuẩn hóa động | Cân bằng âm lượng và hạn chế các đỉnh âm thanh. |
| Phát âm thanh dạng Mono | Trộn các kênh trái và phải thành âm thanh mono. |

### Loại bỏ khoảng lặng

Xem mục [Loại bỏ khoảng lặng và xử lý âm thanh](#8-loai-bo-khoang-lang-va-xu-ly-am-thanh) để biết các điều khiển cơ bản và nâng cao.

### YouTube

| Cài đặt | Mục đích |
| --- | --- |
| Chỉ phát âm thanh của video | Yêu cầu phát âm thanh mà không có hình ảnh. |
| Chất lượng video | Chọn Thấp, Trung bình hoặc Tốt nhất cho việc phát và tải xuống video. |
| Số lượng kết quả tìm kiếm | Đặt mục tiêu ban đầu từ 5 đến 100 kết quả; nhiều kết quả hơn sẽ được tải khi cuộn đến cuối. |
| Hành vi liên kết video+danh sách phát | Hỏi mỗi lần, luôn phát video hoặc luôn mở danh sách phát. |
| Sử dụng yt-dlp để phân giải luồng | Sử dụng thành phần yt-dlp đã tải về thay vì bộ phân giải tích hợp của Luna. |
| Kênh cập nhật yt-dlp | Chọn Ổn định (Stable), Nightly hoặc Master. |
| Kiểm tra bản cập nhật yt-dlp khi khởi động | Thực hiện kiểm tra thành phần ngầm sau khi khởi chạy. |
| Tải xuống thành phần YouTube | Cài đặt hoặc làm mới yt-dlp và các công cụ hỗ trợ. |

### Ghi âm

Trang này thiết lập các giá trị mặc định được sử dụng bởi các phím tắt ghi âm trực tiếp và khởi tạo giao diện ghi âm ở đầu mỗi phiên Luna. Trang bao gồm định dạng âm thanh, tần số lấy mẫu, số kênh, chất lượng âm thanh cho các định dạng nén và thư mục lưu bản ghi âm.

### Phím tắt bàn phím

Trang này liệt kê mọi hành động có thể chạy khi cửa sổ chính của Luna đang giữ tiêu điểm. Mỗi hành động có thể có một phím tắt chính; một vài hành động cũng có thêm một vị trí phím tắt phụ được xác định trước.

1. Chọn một hành động.
2. Kích hoạt **Chỉnh sửa phím tắt chính** hoặc, nếu có sẵn, **Chỉnh sửa phím tắt phụ**.
3. Nhấn tổ hợp phím mong muốn. Nhấn <kbd>Escape</kbd> để hủy thao tác thu nhận phím.
4. Nhấn OK trong Tùy chọn để áp dụng các thay đổi.

Luna từ chối tổ hợp phím đã được gán cho một hành động khác trong cùng bộ phím tắt. Phím tắt cục bộ có thể sử dụng các phím ký tự in được, phím điều hướng, các phím chức năng từ F1 đến F24 cùng các phím bổ trợ Ctrl, Shift hoặc Alt. Sử dụng **Đặt lại về mặc định** để khôi phục toàn bộ trang phím tắt cục bộ sau khi xác nhận.

### Phím tắt toàn cục

Phím tắt toàn cục hoạt động ngay cả khi một ứng dụng khác đang có tiêu điểm. Chúng được giới hạn chủ ý ở các lệnh phát lại, điều hướng, âm lượng và ghi âm hữu ích bên ngoài Luna.

Chọn một hành động, kích hoạt **Chỉnh sửa phím tắt**, và nhấn tổ hợp phím mong muốn. Phím tắt toàn cục cũng có thể sử dụng phím Windows (Win). Windows có thể từ chối một phím tắt đã được hệ điều hành hoặc ứng dụng khác đăng ký trước; Luna sẽ báo cáo những phím tắt không thể đăng ký được.

Phím tắt toàn cục tách biệt hoàn toàn với phím tắt cục bộ. Việc thay đổi một bên sẽ không làm thay đổi bên còn lại.

## 15. Cập nhật ứng dụng

### Kiểm tra thủ công

Chọn **Trợ giúp > Cập nhật > Kiểm tra bản cập nhật ứng dụng**. Một hộp thoại tiến trình có thể hủy sẽ xuất hiện trong khi Luna kiểm tra thông tin bản phát hành.

Nếu có bản phát hành mới hơn sẵn sàng, Luna sẽ hiển thị phiên bản đã cài đặt, phiên bản mới có sẵn và danh sách các thay đổi có thể cuộn được. Chọn **Cập nhật** để tải xuống hoặc **Để sau** để giữ nguyên phiên bản hiện tại.

Luna xác minh rằng trình cài đặt hoặc tệp nén di động thực sự tồn tại trên máy chủ trước khi đưa ra đề xuất cập nhật. Nếu thông tin bản phát hành được công bố trước khi gói tải lên hoàn tất, việc kiểm tra thủ công sẽ nhắc bạn thử lại sau thay vì hiển thị một liên kết tải xuống sẽ bị lỗi.

### Kiểm tra khi khởi động

Bật tùy chọn **Kiểm tra bản cập nhật ứng dụng khi khởi động** để kiểm tra ngầm trong nền. Chế độ này không hiển thị cửa sổ tiến trình hay thông báo "không có bản cập nhật"; lời nhắc chỉ xuất hiện khi có gói tương thích mới hơn sẵn sàng để tải xuống.

### Cài đặt bản cập nhật đã tải xuống

Cửa sổ tải xuống hiển thị tổng dung lượng, dung lượng đã tải, phần trăm và nút Hủy. Sau khi tải xuống thành công, Luna sẽ khởi động tiến trình hỗ trợ cập nhật nhỏ và tự đóng lại:

- Bản cài đặt sẽ khởi chạy trình cài đặt bản cập nhật. Tiến trình cài đặt sẽ hiển thị rõ ràng và Luna sẽ tự động mở lại khi cài đặt hoàn tất.
- Bản di động sẽ thay thế các tệp ứng dụng đã giải nén và mở lại trình phát đã được cập nhật.

Không khởi động một tiến trình Luna khác trong khi bản cập nhật đang được áp dụng.

## 16. Trợ giúp và thông tin bản phát hành

- **Trợ giúp > Hướng dẫn sử dụng** hoặc <kbd>F1</kbd> mở hướng dẫn đã cài đặt phù hợp với ngôn ngữ giao diện của Luna. Nếu không có hướng dẫn theo ngôn ngữ khu vực hoặc ngôn ngữ cơ sở tương ứng, Luna sẽ mở hướng dẫn tiếng Anh.
- **Trợ giúp > Giới thiệu** hiển thị phần mô tả, phiên bản, thông tin bản quyền và liên kết đến trang web của dự án.
- **Trợ giúp > Thông tin bản phát hành** mở trang phát hành GitHub cho phiên bản đã cài đặt trong trình duyệt mặc định của bạn.

Hướng dẫn sử dụng được cài đặt dưới dạng tệp HTML cục bộ và không yêu cầu kết nối Internet. Thông tin bản phát hành và trang web dự án yêu cầu phải có kết nối mạng.

## 17. Cài đặt và dữ liệu người dùng

Luna lưu trữ dữ liệu người dùng tại `%APPDATA%\Luna Player`:

| Tệp | Nội dung |
| --- | --- |
| `settings.json` | Các tùy chọn và phím tắt ghi đè |
| `bookmarks.json` | Dấu trang có tên cho các tệp cục bộ |
| `positions.json` | Vị trí phát theo từng tệp |
| `favorites.json` | Các liên kết YouTube và luồng yêu thích |

Sử dụng **Tùy chọn > Sao lưu và khôi phục > Mở thư mục cài đặt người dùng** để mở chính xác thư mục đó thay vì phải nhập đường dẫn bằng tay.

Không chỉnh sửa các tệp này trong khi Luna đang chạy. Hãy sử dụng các điều khiển xuất và nhập cho cài đặt và dấu trang bất cứ khi nào có thể.

Nếu `settings.json` không thể đọc được hoặc chứa các giá trị không hợp lệ, Luna sẽ khởi động với các giá trị mặc định, hiển thị lý do và bảo vệ tệp hiện có không bị ghi đè. Hãy nhập bản sao lưu cài đặt hợp lệ hoặc sử dụng **Đặt lại cài đặt** để thay thế có chủ đích. Các tệp dấu trang và mục yêu thích không hợp lệ cũng sẽ được báo cáo thay vì bị tự động thay thế trong im lặng.

## 18. Bảng tra cứu phím tắt mặc định đầy đủ

Dưới đây là các phím tắt mặc định của nhà sản xuất. Các tổ hợp phím hiện tại của bạn có thể khác nếu đã tùy chỉnh. Danh sách chính thức cho bản cài đặt của bạn nằm trong **Tùy chọn > Phím tắt bàn phím** và **Phím tắt toàn cục**.

### Tệp, thông tin và Tùy chọn

| Hành động | Phím tắt chính | Phím tắt phụ |
| --- | --- | --- |
| Mở các tệp | <kbd>Ctrl</kbd>+<kbd>O</kbd> | — |
| Mở liên kết luồng mạng | <kbd>Ctrl</kbd>+<kbd>L</kbd> | — |
| Mở tất cả tệp trong thư mục | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd> | — |
| Hiển thị tệp hiện tại trong File Explorer | <kbd>Ctrl</kbd>+<kbd>F</kbd> | — |
| Thuộc tính tệp của Windows | <kbd>Alt</kbd>+<kbd>Enter</kbd> | — |
| Hộp thoại danh sách tệp đang mở | <kbd>F2</kbd> | — |
| Đóng tệp hiện tại | <kbd>Ctrl</kbd>+<kbd>W</kbd> | — |
| Đóng tất cả các tệp | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>W</kbd> | — |
| Tùy chọn | <kbd>Ctrl</kbd>+<kbd>P</kbd> | — |
| Đọc thông tin về tệp hiện tại | <kbd>F</kbd> | — |
| Đọc tiêu đề phương tiện hiện tại | <kbd>I</kbd> | — |
| Thoát Luna | Không có | — |

### Điều hướng danh sách phát và chế độ

| Hành động | Phím tắt chính | Phím tắt phụ |
| --- | --- | --- |
| Tệp trước đó | <kbd>Shift</kbd>+<kbd>Tab</kbd> | <kbd>Page Up</kbd> |
| Tệp tiếp theo | <kbd>Tab</kbd> | <kbd>Page Down</kbd> |
| Tệp đầu tiên | <kbd>Ctrl</kbd>+<kbd>Home</kbd> | — |
| Chuyển đến số thứ tự tệp | <kbd>Ctrl</kbd>+<kbd>G</kbd> | — |
| Tệp cuối cùng | <kbd>Ctrl</kbd>+<kbd>End</kbd> | — |
| Bật/tắt phát ngẫu nhiên | <kbd>Ctrl</kbd>+<kbd>Z</kbd> | — |
| Bật/tắt lặp lại tệp | <kbd>Ctrl</kbd>+<kbd>R</kbd> | — |

### Điều khiển phát và tua

| Hành động | Phím tắt chính | Phím tắt phụ |
| --- | --- | --- |
| Phát hoặc tạm dừng | <kbd>Dấu cách</kbd> | <kbd>Enter</kbd> |
| Tua lùi một bước | <kbd>Trái</kbd> | — |
| Tua tới một bước | <kbd>Phải</kbd> | — |
| Tua lùi hai bước | <kbd>Shift</kbd>+<kbd>Trái</kbd> | — |
| Tua tới hai bước | <kbd>Shift</kbd>+<kbd>Phải</kbd> | — |
| Tua lùi bốn bước | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Trái</kbd> | — |
| Tua tới bốn bước | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Phải</kbd> | — |
| Đầu tệp | <kbd>Home</kbd> | — |
| Cuối tệp | <kbd>End</kbd> | — |
| Chuyển đến thời gian | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>G</kbd> | — |
| Chọn card âm thanh | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>A</kbd> | — |

### Chọn khoảng cách tua

| Hành động | Phím tắt |
| --- | --- |
| Chọn bước 1 giây | <kbd>Shift</kbd>+<kbd>1</kbd> |
| Chọn bước 5 giây | <kbd>Shift</kbd>+<kbd>2</kbd> |
| Chọn bước 10 giây | <kbd>Shift</kbd>+<kbd>3</kbd> |
| Chọn bước 20 giây | <kbd>Shift</kbd>+<kbd>4</kbd> |
| Chọn bước 30 giây | <kbd>Shift</kbd>+<kbd>5</kbd> |
| Chọn bước 1 phút | <kbd>Shift</kbd>+<kbd>6</kbd> |
| Chọn bước 2 phút | <kbd>Shift</kbd>+<kbd>7</kbd> |
| Chọn bước 3 phút | <kbd>Shift</kbd>+<kbd>8</kbd> |
| Chọn bước 5 phút | <kbd>Shift</kbd>+<kbd>9</kbd> |
| Chọn bước 10 phút | <kbd>Shift</kbd>+<kbd>0</kbd> |
| Chọn bước tùy chỉnh | <kbd>Shift</kbd>+<kbd>-</kbd> |

### Nhảy theo phần trăm

| Vị trí | Phím tắt | Vị trí | Phím tắt |
| --- | --- | --- | --- |
| 10% | <kbd>Ctrl</kbd>+<kbd>1</kbd> | 15% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>1</kbd> |
| 20% | <kbd>Ctrl</kbd>+<kbd>2</kbd> | 25% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>2</kbd> |
| 30% | <kbd>Ctrl</kbd>+<kbd>3</kbd> | 35% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>3</kbd> |
| 40% | <kbd>Ctrl</kbd>+<kbd>4</kbd> | 45% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>4</kbd> |
| 50% | <kbd>Ctrl</kbd>+<kbd>5</kbd> | 55% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>5</kbd> |
| 60% | <kbd>Ctrl</kbd>+<kbd>6</kbd> | 65% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>6</kbd> |
| 70% | <kbd>Ctrl</kbd>+<kbd>7</kbd> | 75% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>7</kbd> |
| 80% | <kbd>Ctrl</kbd>+<kbd>8</kbd> | 85% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>8</kbd> |
| 90% | <kbd>Ctrl</kbd>+<kbd>9</kbd> | 95% | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>9</kbd> |
| 100% | <kbd>Ctrl</kbd>+<kbd>0</kbd> | 100% phím phụ | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>0</kbd> |

### Âm lượng và trạng thái giọng đọc

| Hành động | Phím tắt |
| --- | --- |
| Tăng âm lượng | <kbd>Lên</kbd> |
| Giảm âm lượng | <kbd>Xuống</kbd> |
| Đặt âm lượng ở mức tối đa | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Lên</kbd> |
| Đặt âm lượng ở mức tối thiểu | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Xuống</kbd> |
| Đọc âm lượng | <kbd>V</kbd> |
| Đọc thời gian đã phát | <kbd>E</kbd> |
| Đọc thời gian còn lại | <kbd>R</kbd> |
| Đọc tổng thời lượng | <kbd>T</kbd> |
| Đọc phần trăm vị trí | <kbd>P</kbd> |
| Đọc tốc độ phát | <kbd>S</kbd> |
| Chuyển đổi mức Người mới bắt đầu/Nâng cao | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>V</kbd> |

### Tốc độ, cao độ, cân bằng, vòng lặp và bộ lọc

| Hành động | Phím tắt |
| --- | --- |
| Tăng tốc độ phát | <kbd>Ctrl</kbd>+<kbd>Lên</kbd> |
| Giảm tốc độ phát | <kbd>Ctrl</kbd>+<kbd>Xuống</kbd> |
| Đặt lại tốc độ phát | <kbd>Alt</kbd>+<kbd>Y</kbd> |
| Tăng cao độ | <kbd>Shift</kbd>+<kbd>Lên</kbd> |
| Giảm cao độ | <kbd>Shift</kbd>+<kbd>Xuống</kbd> |
| Đặt lại cao độ | <kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>P</kbd> |
| Đọc cao độ | <kbd>Shift</kbd>+<kbd>P</kbd> |
| Chuyển âm thanh sang trái | <kbd>Ctrl</kbd>+<kbd>Trái</kbd> |
| Chuyển âm thanh sang phải | <kbd>Ctrl</kbd>+<kbd>Phải</kbd> |
| Đọc cân bằng âm thanh | <kbd>B</kbd> |
| Bật/tắt loại bỏ khoảng lặng | <kbd>Ctrl</kbd>+<kbd>M</kbd> |
| Đặt điểm bắt đầu vòng lặp A-B | <kbd>[</kbd> |
| Đặt điểm kết thúc vòng lặp A-B | <kbd>]</kbd> |
| Xóa vòng lặp A-B | <kbd>Backspace</kbd> |

### Chỉnh sửa và tệp đã đánh dấu

| Hành động | Phím tắt |
| --- | --- |
| Đổi tên tệp hiện tại | <kbd>Shift</kbd>+<kbd>F2</kbd> |
| Xóa tệp hiện tại | <kbd>Shift</kbd>+<kbd>Delete</kbd> |
| Sao chép tệp hiện tại vào bảng nhớ tạm | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd> |
| Dán/mở tệp hoặc thư mục từ bảng nhớ tạm | <kbd>Ctrl</kbd>+<kbd>V</kbd> |
| Đánh dấu hoặc bỏ đánh dấu tệp hiện tại | <kbd>Ctrl</kbd>+<kbd>K</kbd> |
| Đánh dấu hoặc bỏ đánh dấu tất cả các tệp | <kbd>Ctrl</kbd>+<kbd>A</kbd> |
| Bỏ đánh dấu tất cả các tệp | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>K</kbd> |
| Đọc số lượng tệp đã đánh dấu | <kbd>K</kbd> |
| Sao chép các tệp đã đánh dấu vào thư mục | Không có |
| Di chuyển các tệp đã đánh dấu vào thư mục | Không có |
| Sao chép các tệp đã đánh dấu vào bảng nhớ tạm | Không có |
| Xóa các tệp đã đánh dấu khỏi đĩa | Không có |

### Dấu trang

| Hành động | Phím tắt |
| --- | --- |
| Thêm dấu trang | <kbd>Shift</kbd>+<kbd>M</kbd> |
| Quản lý dấu trang | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>M</kbd> |
| Nhảy đến dấu trang từ 1 đến 9 | <kbd>Alt</kbd>+<kbd>1</kbd> đến <kbd>Alt</kbd>+<kbd>9</kbd> |
| Nhảy đến dấu trang 10 | <kbd>Alt</kbd>+<kbd>0</kbd> |

### YouTube, ghi âm, cập nhật và trợ giúp

| Hành động | Phím tắt |
| --- | --- |
| Mở liên kết YouTube | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd> |
| Tìm kiếm trên YouTube | <kbd>Ctrl</kbd>+<kbd>Y</kbd> |
| Video yêu thích | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd> |
| Tải xuống video YouTube hiện tại | <kbd>Ctrl</kbd>+<kbd>D</kbd> |
| Hiển thị mô tả video hiện tại | <kbd>Alt</kbd>+<kbd>D</kbd> |
| Sao chép liên kết video hiện tại | Không có |
| Mở giao diện ghi âm | <kbd>Alt</kbd>+<kbd>R</kbd> |
| Bắt đầu ghi âm | <kbd>F9</kbd> |
| Tạm dừng hoặc tiếp tục ghi âm | <kbd>F7</kbd> |
| Dừng ghi âm | <kbd>F8</kbd> |
| Mở thư mục chứa bản ghi âm | Không có |
| Mở hướng dẫn sử dụng | <kbd>F1</kbd> |
| Giới thiệu về Luna | Không có |
| Mở thông tin bản phát hành | Không có |
| Kiểm tra bản cập nhật ứng dụng | Không có |
| Cập nhật thành phần YouTube | Không có |

### Phím tắt toàn cục mặc định

Các lệnh này hoạt động trên toàn hệ thống khi Luna đang chạy, ngay cả khi một chương trình khác đang có tiêu điểm.

| Hành động | Phím tắt toàn cục |
| --- | --- |
| Phát hoặc tạm dừng | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Dấu cách</kbd> |
| Tua lùi một bước | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Trái</kbd> |
| Tua tới một bước | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Phải</kbd> |
| Tăng âm lượng | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Lên</kbd> |
| Giảm âm lượng | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Xuống</kbd> |
| Tệp trước đó | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Page Up</kbd> |
| Tệp tiếp theo | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>Page Down</kbd> |
| Bắt đầu ghi âm | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>F9</kbd> |
| Tạm dừng hoặc tiếp tục ghi âm | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>F7</kbd> |
| Dừng ghi âm | <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>F8</kbd> |

## 19. Các định dạng được hỗ trợ

Luna lọc việc chọn tệp và thư mục bằng các phần mở rộng sau. Việc phát lại được xử lý bởi mpv, do đó các định dạng khác được công cụ này hỗ trợ cũng có thể phát được khi mở trực tiếp.

**Âm thanh:** AAC, AC-3, AIFF, ALAC, APE, AU, DTS, E-AC-3, FLAC, M4A, MKA, MP1, MP2, MP3, MPC, OGA, OGG, OGM, Opus, TAK, TrueHD (`.thd`), TTA, WAV, WMA, và WavPack.

**Video:** 3G2, 3GP, AVI, FLV, IVF, M2TS, M4V, MJ2, MKV, MOV, MP4, MPEG, MPG, MXF, OGV, RMVB, TS, WebM, WMV, và Y4M.

**Danh sách phát:** M3U và M3U8, bao gồm cả các luồng HLS.

## 20. Khắc phục sự cố

### Một tệp hoặc thư mục mở ít mục hơn dự kiến

Kiểm tra **Tùy chọn > Chung > Bạn muốn mở gì cùng với các tệp?** Mặc định chỉ mở tệp đã chọn. Các lệnh thư mục chỉ bao gồm những phần mở rộng được nhận diện là phương tiện. Các thư mục không thể truy cập và thư mục hệ thống sẽ bị bỏ qua trong quá trình quét đệ quy.

### Một luồng phát trực tiếp không có thời lượng hoặc phần trăm

Các luồng phát trực tiếp (live stream) thường không công bố độ dài cố định. Việc chuyển đến thời gian, nhảy theo phần trăm, thời gian còn lại và tua đến cuối yêu cầu phải biết trước thời lượng nên có thể sẽ không khả dụng.

### Video YouTube không mở được

Hãy thử các bước sau theo thứ tự:

1. Xác nhận rằng địa chỉ mở được trong trình duyệt và trỏ đến một video hoặc danh sách phát thay vì một kênh.
2. Chọn **Trợ giúp > Cập nhật > Cập nhật thành phần YouTube**.
3. Bật **Sử dụng yt-dlp để phân giải luồng** trong Tùy chọn YouTube.
4. Thử kênh Ổn định (Stable) trước; chỉ sử dụng Nightly hoặc Master nếu kênh Ổn định không xử lý được thay đổi dịch vụ gần đây của YouTube.

### Tính năng ghi âm không tìm thấy chương trình

Tính năng thu âm chương trình yêu cầu Windows 10 phiên bản 2004 trở lên. Chương trình đó cũng phải có phiên âm thanh Windows đang hoạt động. Hãy bắt đầu phát âm thanh trong chương trình đó, mở lại hộp thoại nguồn và chọn chương trình từ danh sách đã làm mới.

### Ghi âm bắt đầu nhưng thiếu một trong các nguồn

Luna vẫn có thể tiếp tục khi có ít nhất một nguồn mở thành công. Hãy đọc cảnh báo liệt kê các nguồn bị lỗi, sau đó kiểm tra xem thiết bị của chúng đã được kết nối, bật và sẵn sàng hay chưa. Dừng ghi âm trước khi chỉnh sửa danh sách nguồn.

### Phím tắt toàn cục không hoạt động

Mở **Tùy chọn > Phím tắt toàn cục** và áp dụng lại tổ hợp phím. Windows không cho phép hai ứng dụng cùng đăng ký một tổ hợp phím toàn cục. Hãy chọn một tổ hợp phím khác nếu Luna báo cáo rằng không thể đăng ký một hoặc nhiều phím tắt.

### Cài đặt không lưu sau khi gặp lỗi khởi động

Luna bảo vệ tệp `settings.json` bị lỗi hoặc không thể đọc được thay vì ghi đè lên nó. Hãy mở Tùy chọn và nhập bản sao lưu cài đặt hợp lệ hoặc chọn **Sao lưu và khôi phục > Đặt lại cài đặt**. Nếu bạn cần tệp gốc để chẩn đoán, hãy sao chép tệp đó từ thư mục cài đặt người dùng trước.

### Dấu trang hoặc mục yêu thích báo cáo dữ liệu không hợp lệ

Tệp lưu trữ không hợp lệ được giữ nguyên thay vì bị tự động thay thế. Hãy mở thư mục cài đặt người dùng, tạo một bản sao của tệp JSON bị ảnh hưởng và khôi phục từ bản sao lưu tốt đã biết. Dấu trang có thể được khôi phục thông qua Tùy chọn. Mục yêu thích hiện yêu cầu khôi phục chính tệp `favorites.json` trong khi Luna đã đóng.

### Hướng dẫn sử dụng không mở được

Hướng dẫn đã cài đặt phải nằm tại `docs\<mã-ngôn-ngữ>\user-guide.html` bên cạnh tệp thực thi. Hãy giải nén lại toàn bộ tệp nén di động hoặc sửa chữa/cài đặt lại ứng dụng nếu thư mục `docs` bị thiếu. Luna sẽ tự động chuyển từ ngôn ngữ khu vực sang ngôn ngữ cơ sở và sau đó sang tiếng Anh nếu cần.

## 21. Nhà phát hành và giấy phép

Luna Player được phát hành bởi **Diamond Star**.

Bản quyền © 2026 Diamond Star.

Mã nguồn gốc của Luna Player được cấp phép theo Giấy phép Apache, Phiên bản 2.0. Bản liên kết mpv được dịch trong `src/Mpv.cs` được cấp phép theo Giấy phép Công cộng Ít Tự do hơn GNU (LGPL), phiên bản 2.1 trở lên.

Luna Player cũng sử dụng các thành phần bên thứ ba được cấp phép riêng, bao gồm mpv, FFmpeg, wxWidgets, Prism, NAudio và YoutubeExplode. Mỗi thành phần vẫn tuân theo giấy phép riêng của nó. Xem `NOTICE.txt`, được cài đặt cùng với Luna Player, để biết các tuyên bố bản quyền, phiên bản thành phần, tên giấy phép và vị trí mã nguồn. Toàn văn các giấy phép liên quan được cài đặt trong thư mục `licenses`.

Tóm tắt giấy phép trong hướng dẫn này được cung cấp nhằm mục đích thuận tiện cho người đọc. `LICENSE.txt`, `NOTICE.txt` và các tệp trong thư mục `licenses` chứa các điều khoản và quy chiếu tác quyền chính thức. Trong kho lưu trữ mã nguồn, các tệp cấp cao nhất tương ứng có tên là `LICENSE` và `NOTICE`.

## 22. Liên hệ và hỗ trợ

Luna Player được phát triển và phát hành bởi Diamond Star. Bạn có thể sử dụng các kênh liên hệ sau:

- **Email:** [ramymaherali55@gmail.com](mailto:ramymaherali55@gmail.com)
- **Telegram:** [Liên hệ với Diamond Star trên Telegram](https://t.me/diamondStar35)
- **Báo cáo lỗi:** [Các vấn đề của Luna Player trên GitHub](https://github.com/diamondStar35/luna_player/issues)
- **Mã nguồn và bản phát hành:** [Luna Player trên GitHub](https://github.com/diamondStar35/luna_player)

Khi báo cáo lỗi, vui lòng gửi kèm phiên bản Luna được hiển thị trong mục **Trợ giúp > Giới thiệu**, phiên bản Windows của bạn, điều bạn mong đợi, điều thực tế đã xảy ra và các bước ngắn nhất để tái hiện sự cố. Hãy gửi kèm văn bản chính xác của bất kỳ thông báo lỗi nào. Vui lòng xóa tên tệp riêng tư, địa chỉ web, thông tin tài khoản và thông tin cá nhân khác khỏi nhật ký hoặc ảnh chụp màn hình trước khi chia sẻ công khai.
