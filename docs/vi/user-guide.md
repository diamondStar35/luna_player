# Hướng dẫn sử dụng Luna Player

Luna Player là trình phát âm thanh và video thân thiện với bàn phím dành cho Windows. Ứng dụng phát phương tiện cục bộ, đĩa DVD-Video và DVD-Audio, luồng mạng, YouTube, Spotify, đài phát thanh internet và IPTV; đọc to phụ đề; tải xuống và chuyển đổi phương tiện; ghi âm âm thanh; đồng thời cung cấp tính năng quản lý tệp, dấu trang, thông báo giọng nói cùng các phím tắt cục bộ và toàn hệ thống có thể tùy cấu hình.

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

Bạn có thể khởi động Luna Player trực tiếp, mở một tệp được hỗ trợ từ Windows hoặc truyền tệp và thư mục thông qua dòng lệnh. Theo mặc định, việc mở thêm phương tiện khi Luna đang chạy sẽ gửi phương tiện đó tới cửa sổ hiện có. Hãy bỏ chọn **Mở các tệp trong cửa sổ hiện có khi Luna đang chạy** trên trang Tùy chọn Chung nếu bạn muốn mỗi tệp mở từ Windows sẽ sử dụng một cửa sổ Luna riêng biệt.

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

Nếu bàn phím, tai nghe hoặc một chương trình khác gửi lệnh đa phương tiện đến sai ứng dụng hoặc gửi hai lần, hãy bật **Vô hiệu hóa điều khiển đa phương tiện hệ thống** trên trang Tùy chọn Chung. Tùy chọn này sẽ xóa Luna khỏi lớp phủ đa phương tiện của Windows và ngăn ứng dụng xử lý các phím đa phương tiện phần cứng.

## 3. Bắt đầu nhanh

Để phát một tệp cục bộ:

1. Nhấn <kbd>Ctrl</kbd>+<kbd>O</kbd>.
2. Chọn một tệp phương tiện, danh sách phát M3U/M3U8 hoặc danh sách phát JSON của Luna.
3. Nhấn <kbd>Dấu cách</kbd> (Space) để tạm dừng hoặc tiếp tục phát.
4. Nhấn mũi tên <kbd>Trái</kbd> hoặc <kbd>Phải</kbd> để tua.
5. Nhấn mũi tên <kbd>Lên</kbd> hoặc <kbd>Xuống</kbd> để thay đổi âm lượng.

Để phát mọi tệp được hỗ trợ trong một thư mục, hãy nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd> và chọn thư mục đó. Sử dụng <kbd>Page Down</kbd> và <kbd>Page Up</kbd> để di chuyển giữa các mục đã nạp.

Để phát video YouTube, nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd> và nhập địa chỉ video. Để tìm kiếm, nhấn <kbd>Ctrl</kbd>+<kbd>Y</kbd>.

Để tìm một đài phát thanh internet, nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>R</kbd>. Để mở Spotify, nhấn <kbd>Alt</kbd>+<kbd>S</kbd>; hãy làm theo hướng dẫn thiết lập tài khoản trong [phần Spotify](#13-spotify) trước khi phát bài hát đầu tiên của bạn.

## 4. Mở phương tiện

### Mở tệp

Chọn **Tệp > Mở tệp...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>O</kbd>. Hộp thoại chấp nhận các tệp âm thanh, video, M3U, M3U8 và tệp danh sách phát JSON của Luna được hỗ trợ.

Cài đặt **Bạn muốn mở gì cùng với các tệp?** kiểm soát điều gì sẽ xảy ra khi một tệp phương tiện được mở từ Windows hoặc từ hộp thoại tệp:

- **Chỉ mở tệp đã chọn** chỉ nạp duy nhất tệp đã chọn.
- **Mở tệp và các tệp trong thư mục chính** nạp các tệp được hỗ trợ trong cùng thư mục và chọn tệp được yêu cầu.
- **Mở tệp cùng các tệp trong thư mục chính và thư mục con** quét thư mục chứa tệp và tất cả các thư mục con bên dưới, nạp mọi tệp phương tiện tìm thấy và chọn tệp được yêu cầu.

Quá trình quét đệ quy có hộp thoại tiến trình có thể hủy và thông báo số tệp phương tiện đã tìm thấy. Các tệp được sắp xếp theo thứ tự sắp xếp tự nhiên của Windows.

Khi nhiều tệp được gửi tới Luna cùng lúc và tùy chọn **Chỉ mở tệp đã chọn** đang được đặt, Luna sẽ nạp tất cả các tệp đã được chọn rõ ràng đó thay vì bỏ qua các tệp còn lại.

### Mở thư mục

Chọn **Tệp > Mở thư mục...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd>.

Với một trong hai chế độ mở tệp đầu tiên, Luna sẽ nạp các tệp phương tiện được hỗ trợ trực tiếp bên trong thư mục đã chọn. Với chế độ thư mục con, ứng dụng sẽ quét đệ quy. Các thư mục hệ thống và thư mục không thể truy cập sẽ được bỏ qua trong quá trình quét đệ quy.

### Mở đĩa DVD

Chọn **Tệp > Mở DVD...**. Trong bộ chọn thư mục, hãy chọn ổ đĩa DVD hoặc thư mục chứa thư mục `VIDEO_TS` hoặc `AUDIO_TS` của đĩa. Bạn cũng có thể chọn trực tiếp thư mục đó. Sử dụng **Loại đĩa** để chọn **DVD-Video** hoặc **DVD-Audio**. DVD-Video phát cả âm thanh lẫn hình ảnh của video; DVD-Audio là định dạng đĩa âm thanh riêng biệt, không phải là tên gọi khác cho phần âm thanh của đĩa DVD video.

Luna đọc các tiêu đề (title) có thể phát của đĩa ở chế độ nền. Tiêu đề là một chương trình riêng biệt trên đĩa, chẳng hạn như một bộ phim, một nội dung bổ sung hoặc một nhóm bản nhạc. Hãy để **Tiêu đề** ở chế độ **Tự động** để phát tiêu đề có thời lượng dài nhất, hoặc chọn một tiêu đề cụ thể khi có nhiều tiêu đề khả dụng. Chọn **Mở** để bắt đầu phát. Nếu quá trình kiểm tra đã hoàn tất, việc phát lại sẽ bắt đầu ngay mà không hiển thị cửa sổ tiến trình mở. Nếu chưa hoàn tất, cửa sổ **Đang mở DVD** sẽ hiển thị tiến trình kiểm tra đang diễn ra; cửa sổ này không bắt đầu đọc lại đĩa từ đầu.

Khi một đĩa có nhiều tiêu đề, các nút Trước đó và Tiếp theo sẽ di chuyển giữa các tiêu đề. Khi chỉ có một tiêu đề có thể phát, chúng sẽ di chuyển giữa các chương của tiêu đề đó. Bạn cũng có thể sử dụng menu **Trình phát > Chương** và các phím tắt chương để điều hướng trong một tiêu đề. Menu đĩa không được hiển thị; hãy sử dụng danh sách tiêu đề và chương để truy cập nội dung trực tiếp.

Luna sẽ báo lỗi nếu vị trí không chứa định dạng đĩa đã chọn hoặc không có tiêu đề nào có thể phát. Nếu DVD-Audio hiển thị thông báo về việc thiếu tiêu đề âm thanh, chỉ thử DVD-Video nếu đĩa đó là DVD video. Một số đĩa DVD-Audio cũng chứa vùng DVD-Video riêng biệt có nội dung khác với vùng âm thanh.

Trình cài đặt và lệnh **Đăng ký phần mở rộng tệp** cũng đăng ký các tác vụ AutoPlay cho DVD. Windows có thể đề xuất Luna khi đĩa DVD được đưa vào máy và hiển thị tùy chọn **Phát DVD bằng Luna Player** trong File Explorer. Hãy chọn Luna trong cài đặt AutoPlay của Windows nếu bạn muốn ứng dụng tự động xử lý khi đưa đĩa vào. Mở đĩa từ Explorer sẽ tự động nhận diện loại đĩa, ưu tiên vùng DVD-Audio khi có cả hai định dạng, và hiển thị **Đang mở DVD** trong khi đọc đĩa. Các đăng ký này chỉ áp dụng cho đĩa DVD; đĩa Blu-ray và Audio CD không thuộc phạm vi của tính năng này.

### Mở luồng mạng

Chọn **Tệp > Mở liên kết...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>L</kbd>. Nhập địa chỉ `http` hoặc `https`.

Sử dụng lệnh này cho các luồng phương tiện trực tiếp và danh sách phát M3U hoặc M3U8 từ xa. Sử dụng **Mở liên kết YouTube...** cho các địa chỉ video và danh sách phát YouTube.

### Mở danh sách phát

Luna hỗ trợ danh sách phát M3U và M3U8 cục bộ và từ xa. Các mục tương đối trong danh sách phát cục bộ được phân giải từ thư mục của danh sách phát; các mục tương đối trong danh sách phát mạng được phân giải từ địa chỉ của danh sách đó.

Bản kê HLS M3U8 được xử lý như một luồng đơn và được chuyển đến công cụ phát. Nó không được mở rộng thành một danh sách các đoạn phương tiện. Các danh sách phát khác sẽ trở thành danh sách các tệp đang mở chứa các đường dẫn cục bộ và liên kết web có thể phát được.

### Thêm vào danh sách hiện tại

Chọn **Tệp > Thêm tệp...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>F</kbd> để chọn một hoặc nhiều tệp. Luna sẽ thêm các tệp đã chọn vào cuối danh sách các tệp đang mở. Bản nhạc hiện tại vẫn tiếp tục phát và các tệp đã có trong danh sách vẫn giữ nguyên thứ tự.

Để thêm một thư mục, hãy chọn **Tệp > Thêm thư mục...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>D</kbd>. Luna sẽ thêm các tệp phương tiện được hỗ trợ trực tiếp bên trong thư mục đó. Ứng dụng không quét các thư mục con, bất kể chế độ mở tệp nào được chọn trong Tùy chọn. Nếu bạn lưu danh sách dưới dạng danh sách phát có tên hoặc tệp JSON, Luna sẽ ghi nhớ chính thư mục đó, vì vậy những thay đổi đối với nội dung của thư mục sẽ được cập nhật khi bạn mở lại danh sách phát.

Tính năng thêm tệp và lưu danh sách hiện tại không khả dụng khi bạn đang trong một phiên duyệt trực tuyến. Hãy rời khỏi phiên đó trước khi sử dụng các lệnh này.

### Danh sách phát đã lưu

Sử dụng **Tệp > Danh sách phát** để lưu giữ các bộ sưu tập mà bạn có thể mở lại mà không cần phải chọn lại tệp mỗi lần. Mỗi danh sách phát đã lưu có một tên và một mô tả tùy chọn. Bạn có thể tạo danh sách phát trống, xây dựng danh sách bằng cách chọn tệp và thư mục, hoặc lưu danh sách bạn đang mở.

Bạn có thể để cửa sổ Danh sách phát của tôi mở trong khi sử dụng cửa sổ trình phát chính. Chọn lại **Danh sách phát của tôi...** sẽ đưa cửa sổ hiện có lên phía trước. Danh sách sẽ cập nhật khi bạn tạo, lưu hoặc xóa một danh sách phát, và việc mở một danh sách phát vẫn để trình quản lý mở để bạn có thể chọn danh sách khác sau này.

Chọn **Mới** hoặc **Chỉnh sửa** sẽ mở trình chỉnh sửa thuộc về Danh sách phát của tôi. Trình chỉnh sửa sẽ tạm thời vô hiệu hóa Danh sách phát của tôi, trong khi cửa sổ trình phát chính vẫn có thể sử dụng được và quá trình phát tiếp tục. Hoàn tất hoặc hủy chỉnh sửa để trả lại quyền điều khiển cho Danh sách phát của tôi.

- **Danh sách phát của tôi...** mở các danh sách phát đã lưu của bạn. Danh sách **Các mục** hiển thị tên và mô tả của chúng. Chọn một danh sách phát để kích hoạt các lệnh Mở, Chỉnh sửa, Xóa và Xuất.
- **Tạo danh sách phát...** mở trình chỉnh sửa danh sách phát với danh sách trống. Nhập tên, thêm các tệp hoặc thư mục bạn muốn và chọn **Lưu**. Danh sách phát sau đó sẽ xuất hiện trong Danh sách phát của tôi.
- **Lưu danh sách phát hiện tại...** sử dụng danh sách các tệp đang mở hiện tại làm điểm bắt đầu. Nhập tên và mô tả (nếu muốn), sau đó chọn **Lưu** để thêm danh sách vào Danh sách phát của tôi. Bạn không cần phải tạo danh sách phát trống rồi thêm lại các tệp đó. Để thay đổi nội dung của danh sách sau này, hãy chọn danh sách đó trong Danh sách phát của tôi và chọn **Chỉnh sửa**.
- **Lưu danh sách phát dưới dạng...** lưu danh sách hiện tại thành một tệp bên ngoài Danh sách phát của tôi. Chọn tên tệp, đích đến và định dạng M3U8, M3U hoặc JSON.
- **Mở danh sách phát...** mở hộp thoại tệp chuẩn để chọn tệp danh sách phát đã lưu và bắt đầu phát các mục của tệp đó.
- **Xuất** trong Danh sách phát của tôi lưu danh sách phát đã chọn vào tệp ngoài. Định dạng JSON giữ nguyên các mục thư mục và mô tả của danh sách phát. M3U và M3U8 xuất các tệp riêng lẻ bên trong mỗi thư mục để sử dụng trong các trình phát khác.
- **Xóa** trong Danh sách phát của tôi sẽ xóa danh sách phát đã chọn sau khi bạn xác nhận. Việc này chỉ xóa định nghĩa danh sách phát; các tệp phương tiện trên đĩa không bị thay đổi.
- **Nhập danh sách phát** sao chép tệp danh sách phát bên ngoài vào Danh sách phát của tôi để tệp đó xuất hiện trong danh sách đã lưu của bạn.

Mở danh sách phát sẽ nạp các mục của danh sách đó. Các tệp bị thiếu và các thư mục trống hoặc không thể truy cập sẽ được báo cáo trong cảnh báo tệp không khả dụng; các mục hợp lệ còn lại vẫn tiếp tục phát. Các mục bị thiếu vẫn được giữ lại trong danh sách phát đã lưu để bạn có thể khôi phục chúng hoặc xóa chúng trong trình chỉnh sửa.

Các tệp được lưu bằng Lưu danh sách phát dưới dạng hoặc Xuất không tự động xuất hiện trong Danh sách phát của tôi. Để mở tệp danh sách phát JSON của Luna, hãy chọn **Tệp > Mở tệp...** và chọn bộ lọc danh sách phát JSON. Danh sách phát của tôi lưu các tệp JSON của riêng mình trong thư mục `Playlists` bên dưới thư mục cài đặt người dùng của Luna; xem **Cài đặt và dữ liệu người dùng** để biết vị trí của thư mục.

### Chỉnh sửa danh sách phát

Nhập tên danh sách phát và mô tả tùy chọn. Tên là bắt buộc khi bạn lưu, và các danh sách phát trong Danh sách phát của tôi phải có tên khác nhau. Mô tả là nơi giải thích danh sách phát chứa những gì hoặc bạn sử dụng danh sách đó khi nào. Bạn có thể lưu danh sách phát trước khi thêm bất kỳ tệp nào và quay lại danh sách đó sau thông qua Danh sách phát của tôi.

Chọn **Thêm > Tệp...** để chọn một hoặc nhiều tệp riêng lẻ trong hộp thoại tệp. Chọn **Thêm > Thư mục...** để đưa vào một thư mục. Danh sách **Các mục** hiển thị từng tệp hoặc thư mục trong các cột **Tên** và **Đường dẫn đầy đủ**, nhờ đó bạn có thể phân biệt các tệp có cùng tên ở các vị trí khác nhau. Bạn cũng có thể nhấn <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>F</kbd> để thêm tệp hoặc <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>D</kbd> để thêm thư mục.

Chọn các mục bạn muốn thay đổi. Khi danh sách mục giữ tiêu điểm, các phím sau sẽ khả dụng:

| Phím | Hành động |
| --- | --- |
| <kbd>Delete</kbd> | Chỉ xóa các mục đã chọn khỏi danh sách phát |
| <kbd>Shift</kbd>+<kbd>Lên</kbd> / <kbd>Shift</kbd>+<kbd>Xuống</kbd> | Di chuyển các mục đã chọn lên / xuống, giữ nguyên thứ tự tương đối của chúng |
| <kbd>Ctrl</kbd>+<kbd>A</kbd> | Chọn tất cả các mục |

Các nút **Xóa**, **Di chuyển lên** và **Di chuyển xuống** thực hiện các thao tác tương tự. Nút Xóa khả dụng khi có ít nhất một mục được chọn. Di chuyển lên khả dụng khi vùng chọn có thể di chuyển mà không vượt qua mục đầu tiên; Di chuyển xuống khả dụng khi vùng chọn có thể di chuyển mà không vượt qua mục cuối cùng. Việc xóa một mục chỉ làm thay đổi danh sách phát và không xóa tệp hoặc thư mục khỏi đĩa.

Chọn **Lưu** để giữ lại các thay đổi về tên, mô tả và mục. Nếu bạn đóng trình chỉnh sửa sau khi thực hiện thay đổi, kể cả bằng **Hủy**, <kbd>Escape</kbd> hoặc <kbd>Alt</kbd>+<kbd>F4</kbd>, Luna sẽ hỏi bạn có muốn lưu chúng hay không. Chọn **Có** để lưu, **Không** để hủy các thay đổi, hoặc **Hủy** để tiếp tục chỉnh sửa. Việc đóng mà không lưu sẽ giữ nguyên danh sách phát đã lưu trước đó.

Nếu lưu thất bại, trình chỉnh sửa vẫn mở cùng các thay đổi của bạn để bạn có thể khắc phục sự cố hoặc thử lại. Khi bạn thoát Luna trong khi trình chỉnh sửa danh sách phát vẫn đang mở, Luna sẽ kiểm tra các thay đổi chưa lưu trước khi thoát. Chọn **Hủy** sẽ giữ Luna và trình chỉnh sửa mở.

### Thư mục danh sách phát và định dạng tệp

Một thư mục vẫn là một mục duy nhất khi bạn lưu danh sách phát trong Danh sách phát của tôi hoặc dưới dạng tệp JSON. Mỗi lần bạn mở danh sách phát đó, Luna sẽ đọc các tệp được hỗ trợ trực tiếp bên trong thư mục và sắp xếp chúng theo tên. Do đó, việc thêm hoặc xóa tệp trong thư mục sẽ thay đổi những gì Luna nạp vào lần tiếp theo bạn mở danh sách. Các thư mục con không được bao gồm. Các tệp bị thiếu và các thư mục trống hoặc không thể truy cập sẽ được báo cáo và các mục của chúng vẫn nằm trong danh sách phát đã lưu.

Chọn **JSON** khi bạn muốn giữ tên, mô tả và các mục thư mục của danh sách phát. Chọn **M3U** hoặc **M3U8** khi bạn muốn có danh sách các đường dẫn và tiêu đề phương tiện riêng lẻ mà bạn có thể mở trong trình phát khác. Cả hai định dạng M3U đều sử dụng UTF-8. Khi bạn xuất một thư mục sang một trong các định dạng này, Luna sẽ ghi các tệp được hỗ trợ hiện có bên trong thư mục đó thay vì ghi chính thư mục. Những thay đổi sau này đối với thư mục sẽ không được phản ánh trong tệp đã xuất đó; hãy xuất lại danh sách phát để cập nhật chúng.

Thứ tự của danh sách phát được bảo toàn khi được lưu hoặc xuất, bao gồm mọi mục lặp lại. Các tệp từ một thư mục xuất hiện cùng nhau tại vị trí của thư mục đó trong danh sách. Việc lưu hoặc xuất danh sách phát không bao giờ di chuyển, sao chép hoặc xóa các tệp phương tiện mà danh sách chứa.

### Mở phương tiện từ bảng nhớ tạm

Sao chép một tệp hoặc thư mục trong File Explorer, quay lại Luna và nhấn <kbd>Ctrl</kbd>+<kbd>V</kbd>. Luna sẽ mở đường dẫn tồn tại đầu tiên trong bảng nhớ tạm theo chế độ mở tệp đã cấu hình.

### Mở lại phương tiện gần đây

Menu con **Tệp > Gần đây** lưu giữ các danh sách riêng biệt cho các tệp, thư mục và danh sách phát đã mở gần đây. Chọn một mục để mở lại. Mỗi danh sách có lệnh **Xóa** riêng, và danh sách trống sẽ hiển thị thông báo bị vô hiệu hóa thay vì một lệnh.

Lịch sử được lưu trữ trong thư mục cài đặt người dùng của Luna. Việc xóa danh sách gần đây chỉ xóa lịch sử của danh sách đó; nó không xóa bất kỳ phương tiện nào.

### Liên kết phần mở rộng tệp

Trình cài đặt có thể đăng ký Luna cho các loại tệp được hỗ trợ. Bạn cũng có thể sử dụng **Tùy chọn > Chung > Đăng ký phần mở rộng tệp** hoặc **Hủy đăng ký phần mở rộng tệp**.

Việc đăng ký sẽ thêm Luna vào danh sách các ứng dụng của Windows cho các phương tiện được hỗ trợ. Windows vẫn có thể yêu cầu bạn chọn Luna trong **Cài đặt > Ứng dụng > Ứng dụng mặc định** trước khi Luna trở thành trình phát mặc định.

## 5. Các điều khiển phát

### Phát và tạm dừng

Nhấn <kbd>Dấu cách</kbd> (Space) hoặc <kbd>Enter</kbd>, chọn **Trình phát > Phát/Tạm dừng**, hoặc sử dụng nút Phát/Tạm dừng trên cửa sổ chính.

Nếu một mục đã kết thúc hoặc đã hủy nạp có thể mở lại được, Phát/Tạm dừng sẽ nạp lại mục đó. Ở mức độ chi tiết Người mới bắt đầu, Luna sẽ đọc "Phát" hoặc "Tạm dừng"; mức Nâng cao sẽ bỏ qua các thông báo trạng thái dài này.

### Tua

Nhấn mũi tên <kbd>Trái</kbd> hoặc <kbd>Phải</kbd> để di chuyển một bước tua. Các bước di chuyển lớn hơn khả dụng mà không cần thay đổi bước tua đã chọn:

- <kbd>Shift</kbd>+<kbd>Trái</kbd> hoặc <kbd>Shift</kbd>+<kbd>Phải</kbd> di chuyển hai bước tua.
- <kbd>Home</kbd> về đầu tệp.
- <kbd>End</kbd> đến cuối tệp.

Khoảng cách tua mặc định là 5 giây. Hãy chọn giá trị khác từ **Trình phát > Khoảng cách tua**, hoặc nhấn <kbd>Shift</kbd> kèm một số:

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

Giá trị được chọn sẽ được lưu ngay lập tức. Thay đổi **Giá trị tua tùy chỉnh (giây)** không tự động kích hoạt giá trị tùy chỉnh đó; hãy chọn nó từ menu hoặc nhấn <kbd>Shift</kbd>+<kbd>-</kbd> khi bạn muốn sử dụng.

### Chuyển đến thời gian

Chọn **Trình phát > Đến thời gian...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>G</kbd>. Nhập giờ, phút và giây phù hợp với thời lượng hiện tại. Luna sẽ ngăn vị trí đã chọn vượt quá thời lượng kết thúc của tệp.

### Nhảy theo phần trăm

Chọn **Trình phát > Nhảy đến phần trăm** hoặc sử dụng các phím tắt phần trăm mặc định:

- <kbd>Ctrl</kbd>+<kbd>1</kbd> đến <kbd>Ctrl</kbd>+<kbd>9</kbd> nhảy tới 10% đến 90%.
- <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>1</kbd> đến <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>9</kbd> nhảy tới 15% đến 95%.
- <kbd>Ctrl</kbd>+<kbd>0</kbd> hoặc <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>0</kbd> nhảy tới 100%.

Nhảy theo phần trăm yêu cầu biết trước thời lượng tổng thể nên sẽ không khả dụng đối với một số luồng phát trực tiếp.

### Nhận thông tin phát

Luna có thể đọc thông tin phát lại mà không làm mất tiêu điểm:

- <kbd>E</kbd> đọc thời gian đã phát.
- <kbd>R</kbd> đọc thời gian còn lại.
- <kbd>T</kbd> đọc tổng thời lượng.
- <kbd>P</kbd> đọc phần trăm hiện tại.
- <kbd>V</kbd> đọc âm lượng hiện tại.
- <kbd>S</kbd> đọc tốc độ hiện tại.
- <kbd>Shift</kbd>+<kbd>P</kbd> đọc mức điều chỉnh cao độ.
- <kbd>B</kbd> đọc giá trị cân bằng âm thanh trái/phải (pan).

### Tên tệp, đường dẫn và tiêu đề phương tiện

Nhấn <kbd>F</kbd> liên tiếp để nhận thông tin chi tiết tăng dần về mục hiện tại:

1. Lần nhấn đầu tiên đọc tên tệp, tên luồng hoặc tiêu đề video YouTube.
2. Lần nhấn thứ hai nhanh chóng đọc đường dẫn tệp cục bộ hoặc địa chỉ luồng đầy đủ.
3. Lần nhấn thứ ba nhanh chóng sao chép đường dẫn hoặc địa chỉ đó vào bảng nhớ tạm.

Nếu bạn tạm dừng lâu hơn một khoảnh khắc ngắn, chuỗi thao tác sẽ bắt đầu lại từ đầu với tên hiển thị.

Nhấn <kbd>I</kbd> để đọc tiêu đề được lưu trong siêu dữ liệu (metadata) của phương tiện. Tiêu đề này tách biệt với tên tệp; một tệp cục bộ có thể không có tiêu đề nhúng hoặc có tiêu đề khác với tên tệp của nó.

### Âm lượng

Nhấn mũi tên <kbd>Lên</kbd> hoặc <kbd>Xuống</kbd> để thay đổi âm lượng theo bước âm lượng đã cấu hình. Bước mặc định là 5 điểm phần trăm.

- <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Lên</kbd> đặt âm lượng ở mức tối đa của Luna.
- <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Xuống</kbd> đặt âm lượng ở mức 5%.
- <kbd>V</kbd> đọc âm lượng hiện tại.

Luna cho phép khuếch đại vượt quá 100%, lên tới 2000%. Mức khuếch đại cao có thể làm tăng tiếng ồn nền và biến dạng âm thanh từ nguồn phát. Tính năng chuẩn hóa động và giới hạn có thể giúp kiềm chế các đỉnh âm thanh, nhưng hãy bắt đầu ở mức vừa phải để bảo vệ thính giác và thiết bị của bạn.

### Tốc độ phát

Nhấn <kbd>Ctrl</kbd>+<kbd>Lên</kbd> hoặc <kbd>Ctrl</kbd>+<kbd>Xuống</kbd> để thay đổi tốc độ phát. Nhấn <kbd>Alt</kbd>+<kbd>Y</kbd> để trở về 1x. Tốc độ được giới hạn từ 0.5x đến 4x, và bước thay đổi có thể tùy cấu hình trong trang Tùy chọn Âm thanh.

### Cao độ

Thay đổi cao độ không làm thay đổi tốc độ phát lại.

- <kbd>Shift</kbd>+<kbd>Lên</kbd> tăng cao độ.
- <kbd>Shift</kbd>+<kbd>Xuống</kbd> giảm cao độ.
- <kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>P</kbd> khôi phục cao độ gốc.
- <kbd>Shift</kbd>+<kbd>P</kbd> đọc mức điều chỉnh hiện tại theo nửa cung (semitones).

Cao độ được giới hạn trong khoảng 12 nửa cung cao hơn hoặc thấp hơn mức gốc. Bước thay đổi có thể cấu hình từ 0.001 đến 12 nửa cung.

### Cân bằng âm thanh stereo

- <kbd>Ctrl</kbd>+<kbd>Trái</kbd> chuyển âm thanh lệch về phía bên trái.
- <kbd>Ctrl</kbd>+<kbd>Phải</kbd> chuyển âm thanh lệch về phía bên phải.
- <kbd>B</kbd> đọc phần trăm cân bằng hiện tại.

Mức 0 là ở giữa (cân bằng), giá trị âm là lệch trái, và giá trị dương là lệch phải. Bước cân bằng có thể cấu hình từ 1 đến 100 điểm phần trăm.

### Chọn thiết bị đầu ra

Chọn **Trình phát > Sound card...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>A</kbd>. Chọn một thiết bị đầu ra âm thanh của Windows và nhấn OK. Thiết bị đã chọn sẽ được lưu ngay lập tức.

### Video toàn màn hình

Chọn **Trình phát > Toàn màn hình** hoặc nhấn <kbd>F11</kbd> để bật hoặc tắt chế độ toàn màn hình. Lệnh này chỉ khả dụng khi mục hiện tại có video. Nhấn <kbd>Escape</kbd> cũng sẽ thoát khỏi chế độ toàn màn hình.

### Chọn luồng âm thanh (audio track)

Các tệp có thể chứa nhiều luồng âm thanh khác nhau, chẳng hạn như các ngôn ngữ khác nhau hoặc bản thuyết minh. Sử dụng **Trình phát > Luồng âm thanh**:

- Nhấn <kbd>Shift</kbd>+<kbd>Page Down</kbd> để chuyển sang luồng tiếp theo.
- Nhấn <kbd>Shift</kbd>+<kbd>Page Up</kbd> để quay lại luồng trước đó.
- Chọn **Danh sách luồng...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>T</kbd> để chọn từ danh sách.

Luna mô tả từng luồng âm thanh bằng tiêu đề, bố cục kênh và ngôn ngữ khi thông tin đó khả dụng. Di chuyển qua luồng âm thanh đầu tiên hoặc cuối cùng sẽ không quay vòng lại.

### Đọc to phụ đề

Menu con **Trình phát > Phụ đề** liệt kê các luồng phụ đề trong tệp hiện tại. Chọn một luồng để đọc to từng dòng phụ đề khi nó xuất hiện, hoặc chọn **Tắt** để dừng đọc phụ đề. Nhấn <kbd>Ctrl</kbd>+<kbd>U</kbd> để bật hoặc tắt luồng phụ đề được chọn gần nhất.

Bạn cũng có thể nạp các phụ đề không được nhúng sẵn trong video:

- Chọn **Nạp từ tệp...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>J</kbd> cho một tệp phụ đề trên máy tính này.
- Chọn **Nạp từ URL...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>J</kbd> cho một địa chỉ `http` hoặc `https`.

Bật **Ghi nhớ phụ đề cho phiên này**, hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>U</kbd>, để giữ ngôn ngữ phụ đề đã chọn xuyên suốt danh sách phát hiện tại. Lựa chọn này chỉ kéo dài cho đến khi Luna đóng lại. Nếu tệp tiếp theo không có phụ đề bằng ngôn ngữ đó, Luna sẽ giữ nguyên lựa chọn phụ đề của tệp đó.

Theo mặc định, Luna đọc xong một dòng phụ đề trước khi đọc dòng tiếp theo. Bật **Ngắt dòng hiện tại khi xuất hiện phụ đề mới** trên trang Tùy chọn Chung nếu việc theo kịp hình ảnh quan trọng hơn việc nghe trọn vẹn từng câu.

### Các chương và sách nói

Sách nói định dạng M4B được mở tương tự như các tệp âm thanh khác. **Trình phát > Chương** liệt kê các chương của tệp hiện tại dưới dạng **số. tiêu đề: thời lượng**. Thời lượng là độ dài của toàn bộ chương. Menu này không khả dụng khi tệp không có chương.

- Nhấn <kbd>Ctrl</kbd>+<kbd>Page Up</kbd> cho chương trước hoặc <kbd>Ctrl</kbd>+<kbd>Page Down</kbd> cho chương tiếp theo. Các lệnh này vẫn nằm trong cuốn sách hiện tại và thông báo chương đích đến.
- Nhấn <kbd>C</kbd> để nghe tên chương hiện tại.
- Chọn một chương trong menu để nhảy đến phần đầu của chương đó. Việc phát lại bình thường không đọc tên chương khi chúng chuyển tiếp tự nhiên.
- Đánh dấu chọn **Dừng phát khi hết chương hiện tại** để tạm dừng một lần khi kết thúc chương đó. Nếu bạn chuyển sang chương khác trong khi tùy chọn này đang được đánh dấu, điểm dừng sẽ theo sau chương đó. Tùy chọn này được ưu tiên hơn tính năng lặp lại và tự động chuyển tiếp, sau đó tự xóa đánh dấu. Nhấn Phát để tiếp tục chuyển sang chương tiếp theo.

Đánh dấu chọn **Trình phát > Chương > Chế độ sách nói** để coi các chương như các mục danh sách phát riêng lẻ. Luna sẽ ghi nhớ lựa chọn này. Việc bật hoặc tắt chế độ này vẫn giữ nguyên vị trí nghe và trạng thái tạm dừng.

Các tệp không có chương sẽ sử dụng chế độ phát bình thường ngay cả khi chế độ sách nói đang được bật. Bạn không cần phải tắt chế độ này trước khi mở nhạc hoặc tệp khác không có chương.

Trong chế độ sách nói, danh sách các tệp đang mở sẽ mở rộng mỗi cuốn sách thành các chương tương ứng; các tệp không có chương vẫn là các mục đơn lẻ. Các lệnh tệp trước/tiếp theo, phát ngẫu nhiên, lặp lại, hành vi khi kết thúc tệp và hẹn giờ ngủ khi kết thúc bài hát sẽ áp dụng cho chương đang hoạt động. Thời gian đã phát, còn lại và tổng thời gian sẽ đề cập đến chương đó. Tua lùi lại sẽ dừng ở đầu chương; khi đến cuối chương (kể cả do tua) sẽ tuân theo hành vi kết thúc đã cấu hình. Vòng lặp A-B và các điểm chọn sẽ nằm trong phạm vi chương, và việc chọn một chương khác sẽ xóa chúng. Thông báo chương tự động tuân theo **Đọc tên tệp khi điều hướng (Trước/Tiếp theo)**.

Dấu trang và vị trí nghe đã ghi nhớ thuộc về toàn bộ cuốn sách. Một dấu trang ở chương khác sẽ chọn chương đó và vị trí đã lưu của nó. Việc sao chép, đổi tên, xóa, đánh dấu hoặc đóng một chương sẽ tác động lên toàn bộ cuốn sách nguồn, bao gồm tất cả các mục chương của cuốn sách đó.

## 6. Làm việc với nhiều tệp

### Trước đó, tiếp theo, đầu tiên và cuối cùng

- <kbd>Page Up</kbd> phát mục trước đó. <kbd>Shift</kbd>+<kbd>Tab</kbd> là phím tắt phụ của lệnh này.
- <kbd>Page Down</kbd> phát mục tiếp theo. <kbd>Tab</kbd> là phím tắt phụ của lệnh này.
- <kbd>Ctrl</kbd>+<kbd>Home</kbd> phát mục đầu tiên.
- <kbd>Ctrl</kbd>+<kbd>End</kbd> phát mục cuối cùng.

Nếu tùy chọn **Quay lại đầu khi mở nhiều tệp** được bật, thao tác Tiếp theo ở mục cuối cùng sẽ quay trở lại mục đầu tiên và thao tác Trước đó ở mục đầu tiên sẽ quay lại mục cuối cùng. Việc tự động chuyển bài khi kết thúc cũng tuân theo quy tắc này.

Bạn có thể tắt các phím tắt phụ trong **Tùy chọn > Phím tắt bàn phím**. Điều này cho phép <kbd>Tab</kbd> và <kbd>Shift</kbd>+<kbd>Tab</kbd> di chuyển giữa các điều khiển trong cửa sổ chính trong khi <kbd>Page Down</kbd> và <kbd>Page Up</kbd> vẫn tiếp tục chuyển bài.

### Chuyển đến số thứ tự tệp

Chọn **Trình phát > Chuyển đến tệp...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>G</kbd>. Nhập một số từ 1 đến tổng số mục đã nạp.

### Hộp thoại các tệp đang mở

Chọn **Tệp > Các tệp đang mở...** hoặc nhấn <kbd>F2</kbd>. Mục hiện tại sẽ được chọn trong danh sách.

- Chọn một mục và kích hoạt **Chuyển đến mục đã chọn**, hoặc nhấn <kbd>Enter</kbd> trên mục đó, để phát mục.
- Kích hoạt **Thông tin danh sách phát** để tính toán số lượng tệp, tổng dung lượng, tổng thời lượng, thời gian đã phát trong danh sách và thời gian còn lại. Quá trình quét có thể hủy được và kết quả sẽ mở trong một cửa sổ văn bản chỉ đọc.

Danh sách được thiết kế để duy trì độ phản hồi mượt mà ngay cả khi nạp số lượng tệp cực lớn.

### Phát ngẫu nhiên

Chọn **Trình phát > Phát ngẫu nhiên** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Z</kbd>. Bật tính năng phát ngẫu nhiên sẽ xây dựng một thứ tự phát ngẫu nhiên trong khi vẫn giữ nguyên mục hiện tại đang được chọn.

Trong khi phát ngẫu nhiên đang bật, các thao tác Trước đó, Tiếp theo, Tệp đầu tiên, Tệp cuối cùng, Chuyển đến tệp và hộp thoại Các tệp đang mở sẽ hoạt động dựa trên thứ tự ngẫu nhiên. Tắt phát ngẫu nhiên sẽ khôi phục lại thứ tự danh sách bình thường trong khi vẫn giữ nguyên mục hiện tại.

### Lặp lại và hành vi khi kết thúc tệp

Chọn **Trình phát > Lặp lại tệp** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>R</kbd> để lặp lại mục hiện tại. Công tắc chuyển đổi tức thời này được ưu tiên áp dụng khi đang bật.

Trang Tùy chọn Âm thanh cũng kiểm soát hành động thông thường khi phát hết một tệp:

- **Chuyển sang tệp tiếp theo** bắt đầu phát mục tiếp theo.
- **Lặp lại tệp** bắt đầu phát lại chính mục đó từ đầu.
- **Không làm gì cả** dừng phát và giữ nguyên ở cuối tệp.

### Ghi nhớ vị trí

Hai cài đặt phục vụ cho các mục đích khác nhau:

- **Ghi nhớ vị trí tệp lần trước** khôi phục tệp cục bộ đang hoạt động và thời gian phát vào lần tiếp theo Luna khởi động.
- **Lưu vị trí hiện tại cho từng tệp** ghi nhớ vị trí riêng biệt cho mọi mục cục bộ và quay lại vị trí đó khi mở lại mục.

Nếu cả hai đều bị tắt, các tệp thông thường sẽ luôn bắt đầu phát từ đầu.

## 7. Vòng lặp A-B

Vòng lặp A-B giúp lặp lại liên tục một đoạn của mục hiện tại.

1. Tua đến vị trí bắt đầu mong muốn và nhấn <kbd>[</kbd>. Thao tác này thiết lập điểm A.
2. Tua đến vị trí tiếp theo và nhấn <kbd>]</kbd>. Thao tác này thiết lập điểm B và bắt đầu lặp từ A đến B.
3. Nhấn <kbd>Backspace</kbd> để xóa vòng lặp và tiếp tục phát từ điểm B.

Trong khi vòng lặp đang hoạt động, thao tác tua thông thường sẽ bị giới hạn trong phạm vi đã chọn. <kbd>Home</kbd> chuyển về điểm A và <kbd>End</kbd> chuyển đến điểm B. Điểm B phải ở sau điểm A.

Vùng chọn áp dụng cho mục hiện tại và sẽ tự động bị xóa khi chuyển sang mục khác.

## 8. Loại bỏ khoảng lặng và xử lý âm thanh

### Bật hoặc tắt tính năng loại bỏ khoảng lặng

Chọn **Trình phát > Bật bộ lọc loại bỏ khoảng lặng** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>M</kbd>. Luna lưu trạng thái bật/tắt ngay lập tức.

Loại bỏ khoảng lặng sẽ rút ngắn các đoạn yên tĩnh trong khi phương tiện đang phát. Tính năng này không làm thay đổi tệp gốc. Kết quả phụ thuộc nhiều vào nguồn phát: một ngưỡng quá mạnh có thể coi giọng nói nhỏ hoặc âm nhạc êm dịu là khoảng lặng.

### Cài đặt khoảng lặng cơ bản

Mở **Tùy chọn > Loại bỏ khoảng lặng**.

- **Thời lượng khoảng lặng tối thiểu (giây)** kiểm soát một đoạn yên tĩnh phải kéo dài bao lâu trước khi nó có thể bị rút ngắn. Tăng giá trị này sẽ giữ lại nhiều khoảng ngắt ngắn hơn.
- **Ngưỡng khoảng lặng** là mức decibel mà dưới mức đó âm thanh được coi là khoảng lặng. Giá trị ít âm hơn (ví dụ: -20) sẽ coi âm thanh lớn hơn là khoảng lặng; giá trị âm nhiều hơn (ví dụ: -50) giới hạn việc loại bỏ ở những âm thanh yên tĩnh hơn.

Các giá trị mặc định là 0.5 giây và -30 dB.

### Cài đặt khoảng lặng nâng cao

Bật **Hiển thị cài đặt nâng cao** để định cấu hình bộ lọc loại bỏ khoảng lặng bên dưới chính xác hơn:

- **Các đoạn im lặng đầu cần cắt**: 0 giữ nguyên phần đầu; 1 xóa khoảng lặng ban đầu cho đến khi tìm thấy âm thanh liên tục. Giá trị cao hơn sẽ tiếp tục qua nhiều đoạn âm thanh hơn.
- **Thời lượng âm thanh yêu cầu trước khi dừng cắt đoạn đầu**: âm thanh phải duy trì trên ngưỡng bao lâu trước khi Luna giữ lại.
- **Các đoạn im lặng cần cắt sau khi âm thanh bắt đầu**: -1 rút ngắn mọi khoảng ngắt sau đó đủ điều kiện, 0 giữ nguyên khoảng lặng sau đó, và giá trị dương giới hạn số lượng đoạn được rút ngắn.
- **Độ dài khoảng lặng bên trong tối thiểu**: khoảng ngắt đủ điều kiện tối thiểu sau khi âm thanh đã bắt đầu.
- **Khoảng dừng cần giữ lại sau khoảng lặng đã cắt**: giữ lại một phần của mỗi khoảng dừng để các từ không bị dính liền vào nhau.
- **Kích thước cửa sổ phát hiện**: khoảng thời gian được sử dụng cho mỗi lần đo độ lớn âm thanh. Cửa sổ lớn hơn đo ổn định hơn; cửa sổ nhỏ hơn phản ứng nhanh hơn.
- **Chế độ phát hiện**: Peak (Đỉnh) phản ứng với mẫu âm thanh lớn nhất và các âm thanh ngắn; RMS sử dụng năng lượng trung bình để phát hiện mượt mà hơn.

Hãy thực hiện từng thay đổi nhỏ và thử nghiệm trên nội dung thực tế. Bộ lọc sẽ tác động đến việc phát lại ngay sau khi bạn nhấn chấp nhận Tùy chọn.

### Chuẩn hóa, giới hạn và mono

Trang **Tùy chọn > Âm thanh > Phát lại** chứa hai bộ lọc bổ sung:

- **Bật chuẩn hóa động và giới hạn âm lượng** làm đồng đều độ lớn thay đổi và kiềm chế các đỉnh âm thanh để giảm hiện tượng méo tiếng (clipping).
- **Phát âm thanh dạng Mono** kết hợp kênh trái và kênh phải thành một đầu ra mono duy nhất.

Cả hai tùy chọn đều không làm thay đổi tệp nguồn.

### Âm thanh không gian cho tai nghe (Headphone spatialization)

Âm thanh không gian cho tai nghe tạo cảm giác như âm thanh phát ra từ các loa ảo xung quanh đầu bạn. Tính năng sử dụng hàm truyền liên quan đến đầu (head-related transfer function, hay HRTF): các phép đo về cách âm thanh từ các hướng khác nhau truyền đến tai người nghe. Hãy nghe bằng tai nghe để cảm nhận hiệu ứng. Tính năng này thay đổi âm thanh trong khi phát và không làm thay đổi tệp phương tiện gốc.

#### Tải xuống bộ dữ liệu HRTF

**Luna Player không đi kèm sẵn bất kỳ bộ dữ liệu HRTF nào.** Hãy tải xuống một tệp `.sofa` tương thích riêng và lưu trong thư mục mà tệp sẽ luôn khả dụng. SOFA là định dạng tệp được sử dụng để lưu trữ các phép đo; hãy chọn tệp chứa phản ứng `SimpleFreeFieldHRIR` để nghe bằng tai nghe.

SADIE D1 là điểm khởi đầu rất tốt và cũng được cung cấp trong các tệp HRTF của Steam Audio. Các liên kết sau đây tải xuống trực tiếp các tệp SOFA:

| Bộ dữ liệu | Tải xuống và sử dụng |
| --- | --- |
| SADIE D1 | [Tải sadie_d1.sofa từ Steam Audio](https://raw.githubusercontent.com/ValveSoftware/steam-audio/master/core/data/hrtf/sadie_d1.sofa). Bắt đầu tại đây nếu bạn chưa có HRTF yêu thích. |
| SADIE H12 | [Tải sadie_h12.sofa từ Steam Audio](https://raw.githubusercontent.com/ValveSoftware/steam-audio/master/core/data/hrtf/sadie_h12.sofa). Bộ đo thay thế để so sánh với D1. |
| SADIE D2 | [Tải D2 từ cơ sở dữ liệu SOFA](https://sofacoustics.org/data/database/sadie/D2_48K_24bit_256tap_FIR_SOFA.sofa). Lựa chọn thay thế khác nếu D1 không mang lại cảm giác định hướng rõ ràng cho bạn. |

Bạn có thể tìm thêm các phép đo trong [cơ sở dữ liệu SADIE](https://www.york.ac.uk/sadie-project/database.html) và [bộ sưu tập dữ liệu SOFA](https://sofacoustics.org/data/database/). Hãy tải trực tiếp tệp `.sofa` thay vì lưu trang web mô tả tệp. Nếu trình duyệt mở tệp thay vì tải về, hãy sử dụng lệnh lưu của trình duyệt hoặc lệnh **Lưu liên kết dưới dạng...** và giữ nguyên phần mở rộng `.sofa`.

Không có bộ dữ liệu đơn lẻ nào nghe hay nhất cho tất cả mọi người. Hãy so sánh một vài bộ với cùng một bản ghi quen thuộc ở mức âm lượng tương đương, lắng nghe các vị trí trái/phải, trước/sau và trên/dưới rõ ràng. Tệp tải về lớn hơn hoặc tốc độ lấy mẫu đo cao hơn tự nó không làm cho bộ dữ liệu phù hợp hơn với đôi tai của bạn.

#### Thiết lập âm thanh không gian

1. Mở **Tùy chọn > Âm thanh > Âm thanh không gian cho tai nghe**.
2. Chọn **Bật âm thanh không gian cho tai nghe** để hiển thị các điều khiển khác.
3. Chọn **Duyệt...** và chọn tệp `.sofa` đã tải xuống.
4. Đặt độ khuếch đại, các góc ban đầu và bước phím tắt khi cần, sau đó chọn **OK**.

Âm thanh không gian mặc định tắt. Nếu bạn muốn tắt tính năng này khi khởi động nhưng vẫn muốn sẵn sàng từ menu Trình phát, hãy chọn một tệp và định cấu hình các giá trị mặc định, sau đó bỏ chọn **Bật âm thanh không gian cho tai nghe** trước khi chọn OK. Các trường khác sẽ bị ẩn khi hộp kiểm bị bỏ chọn, nhưng các giá trị của chúng vẫn được giữ lại.

| Cài đặt | Mục đích |
| --- | --- |
| Độ khuếch đại (dB) | Điều chỉnh đầu ra từ -20 đến 40 dB, ban đầu là 0. Giảm xuống nếu kết quả quá lớn hoặc bị méo tiếng. Cho phép nhập số thập phân. |
| Góc xoay (độ) | Góc ngang ban đầu, từ -360 đến 360, ban đầu là 0. Giá trị dương xoay sang phải; giá trị âm xoay sang trái. |
| Góc nâng (độ) | Góc dọc ban đầu, từ -90 đến 90, ban đầu là 0. Giá trị dương nâng âm thanh lên; giá trị âm hạ âm thanh xuống. |
| Bước xoay (độ) | Mức thay đổi thực hiện bởi mỗi lệnh xoay, từ 1 đến 90 độ. Mặc định là 5. |
| Bước nâng (độ) | Mức thay đổi thực hiện bởi mỗi lệnh nâng, từ 1 đến 90 độ. Mặc định là 5. |

Góc xoay, góc nâng và các bước thay đổi sử dụng điều khiển số nguyên. Độ khuếch đại sử dụng trường văn bản để bạn có thể nhập số thập phân.

#### Thay đổi hướng trong khi phát

Sử dụng **Trình phát > Âm thanh không gian cho tai nghe** hoặc các phím tắt mặc định sau:

| Lệnh | Phím tắt mặc định | Hiệu ứng |
| --- | --- | --- |
| Bật âm thanh không gian cho tai nghe | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>H</kbd> | Bật hoặc tắt hiệu ứng cho phiên này. Không khả dụng cho đến khi đường dẫn SOFA được định cấu hình trong Tùy chọn. |
| Xoay sang trái | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Trái</kbd> | Trừ đi bước xoay. |
| Xoay sang phải | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Phải</kbd> | Cộng thêm bước xoay. |
| Tăng góc nâng | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Lên</kbd> | Cộng thêm bước nâng. |
| Giảm góc nâng | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Xuống</kbd> | Trừ đi bước nâng. |

Các lệnh định hướng khả dụng trong khi âm thanh không gian đang được bật. Góc xoay sẽ quay vòng quanh một vòng tròn đầy đủ: lùi một bước mặc định sang trái từ 0 độ sẽ thành 355 độ. Góc nâng dừng ở -90 độ (ngay bên dưới bạn) và 90 độ (ngay phía trên bạn). Gần các vị trí thẳng đứng đó, việc thay đổi góc xoay có thể tạo ra ít sự khác biệt về âm thanh nghe được. Để nghe chuyển động quanh đầu rõ hơn, hãy đưa góc nâng trở lại gần mức 0.

Luna thông báo mỗi lần thay đổi. Ví dụ: mức Người mới bắt đầu đọc "Góc xoay 270 độ" và mức Nâng cao đọc "270 độ". Những thay đổi này có hiệu lực cho phiên hiện tại và không thay thế các giá trị mặc định trong Tùy chọn. Việc lưu các tùy chọn không liên quan vẫn giữ nguyên trạng thái bật và các góc tạm thời này. Thay đổi các giá trị mặc định của âm thanh không gian sẽ áp dụng các cài đặt đã chỉnh sửa; việc tắt và bật hiệu ứng từ menu Trình phát sẽ giữ lại các góc của phiên hiện tại.

Tạm dừng, tua và chuyển giữa các tệp vẫn bảo toàn các cài đặt âm thanh không gian của bạn. Việc bật hoặc tắt hiệu ứng có thể gây gián đoạn ngắn trong khi quá trình xử lý âm thanh thay đổi, và việc nạp một bộ dữ liệu lớn có thể mất nhiều thời gian hơn. Việc thay đổi hướng không yêu cầu phải chọn lại tệp.

Hãy để tùy chọn **Phát âm thanh dạng Mono** tắt để giữ nguyên các tín hiệu stereo được tạo bởi hiệu ứng. Hãy tắt âm thanh không gian khi nghe qua loa hoặc với âm thanh đã là binaural. Giữ tệp SOFA tại đường dẫn đã chọn: Luna không sao chép nó vào thư mục cài đặt hay đưa vào bản sao lưu cài đặt. Nếu tệp bị di chuyển hoặc bị xóa, hãy chọn vị trí mới của tệp trong Tùy chọn. Nếu không thể áp dụng hiệu ứng, Luna sẽ báo lỗi và tắt hiệu ứng cho phiên hiện tại.

### Bộ cân bằng âm thanh (Equalizer)

Bộ cân bằng âm thanh nâng và cắt các dải tần số riêng lẻ. Chọn **Trình phát > Bộ cân bằng âm thanh** và chọn một mục từ danh sách. Thay đổi có hiệu lực ngay lập tức và được ghi nhớ giữa các phiên làm việc.

Danh sách bắt đầu với **Tắt**, tiếp theo là sáu cấu hình cho các vấn đề nghe phổ biến:

- **Tăng âm trầm (Bass Boost)** và **Giảm âm trầm (Bass Reduce)** nâng hoặc hạ âm trầm. Hiệu ứng mạnh nhất ở dưới 100 Hz, vẫn rõ rệt ở 170 Hz và giảm dần đến 600 Hz, do đó nó bao phủ dải tần của trống bass và đàn guitar bass mà không làm đục giọng nói.
- **Tăng âm bổng (Treble Boost)** nâng mọi thứ từ khoảng 4 kHz trở lên.
- **Tai nghe (Headphones)** tăng thêm trọng lượng cho âm trầm, làm dịu dải 3.5 kHz nơi tai nghe dễ bị gắt và mở rộng dải âm cao.
- **Giọng nói (Voice)** loại bỏ tiếng ầm và độ bí, đồng thời nâng dải hiện diện (presence) - dải tần giúp phụ âm trở nên rõ ràng. Cấu hình này phù hợp với sách nói, podcast và bài giảng hơn là âm nhạc.
- **Độ lớn (Loudness)** nâng cả hai đầu âm trầm/bổng và giữ nguyên dải giữa, thích hợp khi nghe ở âm lượng nhỏ nơi tai người kém nhạy cảm nhất với hai đầu dải tần.

Tiếp theo là các cấu hình thể loại nhạc: Cổ điển (Classical), Club, Nhảy (Dance), Trực tiếp (Live), Pop, Reggae, Rock, Ska, Nhẹ nhàng (Soft), Soft Rock và Techno. **Tùy chỉnh (Custom)** ở cuối cùng. Cấu hình này không thay đổi gì cho đến khi bạn chỉnh sửa nó và là điểm bắt đầu cho các cấu hình của riêng bạn.

Một cấu hình nâng dải tần sẽ làm cho âm thanh lớn hơn chứ không chỉ khác biệt, và không có dải nào bị giảm âm lượng tương đối để làm cho sự thay đổi có vẻ lớn hơn. Do đó, các nội dung đã được thu âm sát giới hạn có thể bị vỡ tiếng (clip). Tùy chọn **Bật chuẩn hóa động và giới hạn âm lượng** trên trang Tùy chọn Âm thanh sẽ ngăn chặn điều này và mặc định được bật; nếu tắt tùy chọn đó, hãy giảm âm lượng trình phát xuống.

**Tắt** sẽ làm phẳng mọi băng tần thay vì gỡ bỏ bộ cân bằng âm thanh. Cấu hình bạn sử dụng gần nhất sẽ được ghi nhớ trong khi bộ cân bằng âm thanh tắt, vì vậy việc chọn lại sẽ khôi phục cấu hình đó.

### Chỉnh sửa cấu hình

Chọn **Trình phát > Bộ cân bằng âm thanh > Chỉnh sửa cấu hình hiện tại** để mở cấu hình mà bộ cân bằng âm thanh đang áp dụng. Mọi cấu hình đều có thể chỉnh sửa, kể cả những cấu hình mà Luna cung cấp sẵn.

Hộp thoại chứa một nhóm cho mỗi băng tần: một dải shelf trầm (low shelf), mười lăm băng tần từ 25 Hz đến 16 kHz, và một dải shelf bổng (high shelf). Mỗi nhóm chứa ba trường:

- **Tần số (Frequency)** là tâm của băng tần, hoặc điểm chuyển tiếp của dải shelf.
- **Độ khuếch đại (Gain)** là mức nâng hoặc hạ băng tần, tính bằng decibel, từ -20 đến 20.
- **Độ rộng (Q)** là độ rộng của băng tần, từ 0.1 đến 10. Giá trị lớn hơn sẽ có phạm vi hẹp hơn.

Khi cấu hình đang được chỉnh sửa chính là cấu hình đang được sử dụng, mỗi thay đổi sẽ được áp dụng ngay khi bạn gõ, vì vậy bạn có thể cảm nhận bằng tai trước khi quyết định lưu lại. **Lưu** giữ lại kết quả; **Hủy** khôi phục cấu hình như cũ.

**Cấu hình cơ sở** điền trước mọi băng tần từ một cấu hình khác. Hãy sử dụng nó để bắt đầu từ một cấu hình gần giống với mong muốn của bạn. **Đặt lại về mặc định** đưa các băng tần trở lại nguyên bản như khi cấu hình được cung cấp.

Chỉnh sửa cấu hình do Luna cung cấp chỉ ghi lại các giá trị bạn đã thay đổi, vì vậy các băng tần bạn không can thiệp vẫn tiếp tục tuân theo cấu hình của Luna và những cải tiến sau này đối với cấu hình đó vẫn sẽ cập nhật tới bạn. Đặt lại sẽ hủy bỏ các thay đổi đó. Tên của cấu hình có sẵn không thể thay đổi.

### Quản lý các cấu hình

Chọn **Trình phát > Bộ cân bằng âm thanh > Quản lý cấu hình** để xem danh sách đầy đủ.

- **Mới** tạo một cấu hình, bắt đầu từ cấu hình cơ sở bạn chọn trong trình chỉnh sửa.
- **Chỉnh sửa** mở cấu hình đã chọn.
- **Xóa** xóa cấu hình sau khi xác nhận.
- **Đóng** rời khỏi danh sách.

Chỉnh sửa và Xóa áp dụng cho các cấu hình do bạn tạo và bị ẩn khi một cấu hình có sẵn của Luna đang được chọn; hãy sử dụng **Chỉnh sửa cấu hình hiện tại** cho các cấu hình đó. Để sao chép một cấu hình, hãy tạo cấu hình mới và chọn cấu hình gốc làm cơ sở của nó.

Các cấu hình bạn tạo sẽ xuất hiện trong menu Bộ cân bằng âm thanh cùng với các cấu hình còn lại và được lưu trữ trong tệp `equalizer.json`, nằm cạnh tệp cài đặt.

## 9. Quản lý tệp cục bộ

Các lệnh đổi tên, xóa, mở thư mục chứa và thuộc tính Windows chỉ hoạt động với các tệp cục bộ. Chúng không khả dụng cho các luồng mạng và phương tiện YouTube.

### Hiển thị tệp trong Windows

- Nhấn <kbd>Ctrl</kbd>+<kbd>F</kbd> hoặc chọn **Tệp > Mở thư mục chứa** để mở File Explorer với tệp hiện tại được chọn.
- Nhấn <kbd>Alt</kbd>+<kbd>Enter</kbd> hoặc chọn **Tệp > Thuộc tính tệp...** để mở hộp thoại thuộc tính tệp của Windows.

### Đổi tên tệp

Chọn **Tệp > Đổi tên tệp...** hoặc nhấn <kbd>F6</kbd>. Nhập tên tệp mới không bao gồm phần mở rộng tệp. Luna sẽ bảo toàn phần mở rộng tệp gốc và cập nhật danh sách các tệp đang mở, các dấu trang liên quan cùng với lịch sử vị trí phát.

### Xóa tệp

Chọn **Tệp > Xóa tệp** hoặc nhấn <kbd>Shift</kbd>+<kbd>Delete</kbd>. Sau khi xác nhận, Luna sẽ di chuyển tệp vào Thùng rác (Recycle Bin) của Windows, xóa các dấu trang và dữ liệu vị trí liên quan của tệp đó, đồng thời chuyển sang tệp tiếp theo.

### Sao chép tệp

- Chọn **Tệp > Sao chép tệp** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>C</kbd> để đặt đối tượng tệp vào bảng nhớ tạm của Windows. Sau đó bạn có thể dán tệp vào File Explorer.
- Chọn **Tệp > Sao chép đường dẫn tệp** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd> để sao chép đường dẫn đầy đủ dạng văn bản vào bảng nhớ tạm.

### Đóng tệp

Chọn **Tệp > Đóng tệp** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>W</kbd>. Tệp hiện tại sẽ được gỡ khỏi danh sách các tệp đang mở và quá trình phát sẽ chuyển sang mục tiếp theo.

## 10. Các tệp đã đánh dấu và thao tác hàng loạt

Đánh dấu cho phép bạn gom các tệp cục bộ trong quá trình điều hướng và sau đó thao tác trên tất cả các tệp đó cùng một lúc.

- Nhấn <kbd>Ctrl</kbd>+<kbd>K</kbd> để đánh dấu hoặc bỏ đánh dấu tệp hiện tại.
- Nhấn <kbd>Ctrl</kbd>+<kbd>A</kbd> để đánh dấu tất cả các tệp đã nạp hoặc bỏ đánh dấu tất cả khi mọi tệp đều đã được đánh dấu.
- Nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>K</kbd> để xóa tất cả các đánh dấu.
- Nhấn <kbd>K</kbd> để nghe số lượng tệp đã đánh dấu.

Khi có ít nhất một tệp được đánh dấu, menu **Thao tác cho các tệp đã đánh dấu** sẽ khả dụng:

- **Sao chép vào bảng nhớ tạm** đặt các tệp đã đánh dấu vào bảng nhớ tạm để dán trong File Explorer.
- **Sao chép đường dẫn** sao chép danh sách đường dẫn phân tách bằng dòng mới vào bảng nhớ tạm.
- **Sao chép vào thư mục...** sao chép các tệp vào thư mục đích đã chọn.
- **Di chuyển vào thư mục...** di chuyển các tệp vào thư mục đích đã chọn và cập nhật danh sách các tệp đang mở.
- **Xóa...** yêu cầu xác nhận và di chuyển tất cả các tệp đã đánh dấu vào Thùng rác của Windows.

Hộp thoại tiến trình cho các thao tác hàng loạt có thể hủy được và hiển thị trạng thái hoàn thành.

## 11. Dấu trang

Dấu trang lưu lại các vị trí phát có đặt tên cho các tệp cục bộ. Chúng được lưu trữ theo từng tệp và không khả dụng cho các luồng mạng hoặc video YouTube.

### Thêm dấu trang

1. Phát hoặc tua đến vị trí mong muốn.
2. Chọn **Dấu trang > Thêm dấu trang mới** hoặc nhấn <kbd>Shift</kbd>+<kbd>M</kbd>.
3. Chấp nhận tên dựa trên thời gian được tạo tự động hoặc nhập một tên mang tính mô tả.

Các dấu trang được sắp xếp theo vị trí phát. Mười dấu trang đầu tiên theo thứ tự đó sẽ chiếm các vị trí được gán phím tắt trực tiếp.

### Nhảy đến dấu trang theo số

Nhấn <kbd>Alt</kbd>+<kbd>1</kbd> đến <kbd>Alt</kbd>+<kbd>0</kbd> để nhảy đến dấu trang từ 1 đến 10 trong tệp hiện tại.

### Quản lý dấu trang

Chọn **Dấu trang > Quản lý dấu trang...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>B</kbd>. Hộp thoại hiển thị danh sách các dấu trang của tệp hiện tại cùng với các nút **Chuyển đến**, **Đổi tên**, **Xóa** và **Xóa tất cả**.

Dấu trang được lưu trong tệp `bookmarks.json` bên dưới thư mục cài đặt người dùng của Luna.

## 12. YouTube

Các tính năng YouTube yêu cầu kết nối internet. Việc phát, duyệt kênh và tải xuống cũng yêu cầu các thành phần YouTube có sẵn từ trang Tùy chọn YouTube. Tính khả dụng có thể thay đổi khi YouTube thay đổi dịch vụ của họ.

### Mở liên kết YouTube

Chọn **Tệp > Mở liên kết YouTube...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd>. Nhập địa chỉ video hoặc danh sách phát. Địa chỉ kênh không được lệnh này chấp nhận.

Một số địa chỉ YouTube xác định cả video và danh sách phát. Luna có thể hỏi bạn muốn phát video hay mở danh sách phát, hoặc tuân theo hành vi cố định được chọn trong Tùy chọn YouTube.

### Tìm kiếm trên YouTube

Chọn **Tệp > Tìm kiếm trên YouTube...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Y</kbd>. Nhập văn bản tìm kiếm, sau đó di chuyển qua danh sách kết quả. Theo mặc định, Luna cung cấp các gợi ý tìm kiếm khi bạn gõ. Bạn có thể tắt tính năng này trong Tùy chọn YouTube nếu không muốn gửi yêu cầu mạng ở chế độ nền khi nhập truy vấn.

- Nhấn <kbd>Enter</kbd> trên một kết quả đã chọn hoặc kích hoạt **Phát** để phát cùng video.
- Nhấn <kbd>Ctrl</kbd>+<kbd>Enter</kbd> hoặc kích hoạt **Phát dưới dạng âm thanh** để chỉ phát âm thanh.
- Kích hoạt **Tải xuống** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>D</kbd> để chọn cách lưu tệp.
- Nhấn <kbd>Ctrl</kbd>+<kbd>D</kbd> để tải xuống ngay lập tức với các giá trị mặc định đã lưu.
- Nhấn <kbd>Ctrl</kbd>+<kbd>Q</kbd> để thêm vào hàng đợi tải xuống.
- Nhấn <kbd>Ctrl</kbd>+<kbd>Dấu cách</kbd> để thêm vào hoặc xóa khỏi danh sách Yêu thích.
- Nhấn <kbd>Ctrl</kbd>+<kbd>C</kbd> để sao chép liên kết của kết quả đã chọn.
- Nhấn <kbd>Ctrl</kbd>+<kbd>B</kbd> để mở kết quả trong trình duyệt mặc định.
- Nhấn <kbd>Ctrl</kbd>+<kbd>N</kbd> để mở kênh của người tải lên trong trình duyệt mặc định.
- Nhấn <kbd>Ctrl</kbd>+<kbd>G</kbd> để duyệt kênh đó ngay bên trong Luna.
- Mở menu ngữ cảnh để thực hiện các thao tác tương tự về yêu thích, sao chép, trình duyệt, kênh, tải xuống và hàng đợi.

Khi vùng chọn đến cuối trang kết quả hiện tại, Luna sẽ tự động yêu cầu thêm kết quả cho đến khi YouTube không còn kết quả nào để trả về.

Cửa sổ kết quả danh sách phát và kênh hoạt động theo cách tương tự. Nút Tiếp theo di chuyển tiếp qua phiên kết quả YouTube đang hoạt động, nạp video tiếp theo khi cần thiết. Nhấn <kbd>Escape</kbd> trong cửa sổ chính sau khi bắt đầu phát có thể quay lại danh sách kết quả liên quan khi phiên đó vẫn còn khả dụng.

Kết quả kênh được sắp xếp thành các tab. Một tab sẽ được nạp khi bạn mở lần đầu tiên và sau đó được giữ lại trong khi cửa sổ kênh vẫn mở.

### Chất lượng phát lại

Mở **Tùy chọn > YouTube**.

- **Chất lượng video** chọn chiều cao hình ảnh ưu tiên cho quá trình phát được bắt đầu bằng <kbd>Enter</kbd>.
- **Chất lượng âm thanh** chọn tốc độ bit ưu tiên cho quá trình chỉ phát âm thanh được bắt đầu bằng <kbd>Ctrl</kbd>+<kbd>Enter</kbd>.

Đây là các cài đặt phát lại. Chất lượng tải xuống và định dạng tải xuống có các giá trị mặc định riêng trên cùng trang Tùy chọn này.

### Tùy chọn video

Trong khi một video YouTube đang hoạt động, menu **Tùy chọn video** cung cấp:

- **Tải xuống...** hoặc <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>D</kbd>: chọn tên, thư mục đích, định dạng, chất lượng và các tùy chọn xử lý bổ sung.
- **Tải xuống trực tiếp** hoặc <kbd>Ctrl</kbd>+<kbd>D</kbd>: bắt đầu tải ngay lập tức với các giá trị mặc định từ Tùy chọn YouTube.
- **Thêm vào hàng đợi tải xuống** hoặc <kbd>Ctrl</kbd>+<kbd>Q</kbd>: thêm video hiện tại với các giá trị mặc định đó mà không cần khởi động hàng đợi.
- **Mô tả video...** hoặc <kbd>Alt</kbd>+<kbd>D</kbd>: lấy tiêu đề cùng mô tả của người tải lên và hiển thị trong một cửa sổ văn bản chỉ đọc.
- **Sao chép liên kết video**: sao chép địa chỉ xem YouTube cố định thay vì địa chỉ luồng phương tiện tạm thời.
- **Mở video trong trình duyệt** và **Mở kênh trong trình duyệt**: mở trang YouTube tương ứng trong trình duyệt mặc định.
- **Chuyển đến kênh**: duyệt kênh của người tải lên ngay bên trong Luna.

Tải xuống lặp lại sẽ không ghi đè lên tệp hiện có có cùng tên; Luna sẽ tự động chọn một tên kèm số chưa được sử dụng.

### Chọn tùy chọn tải xuống

Hộp thoại Tải xuống bắt đầu với các giá trị mặc định từ Tùy chọn YouTube và cho phép bạn thay đổi chúng cho từng mục. Bạn có thể chọn:

- Tên tệp và thư mục đích.
- Đầu ra video hoặc chỉ âm thanh.
- Chất lượng video và định dạng MP4 hoặc MKV cho video.
- MP3, M4A, Opus, FLAC hoặc WAV cùng tốc độ bit âm thanh cho đầu ra chỉ âm thanh.
- Số lượng đoạn mà yt-dlp tải về cùng lúc.
- Có nhúng siêu dữ liệu, hình thu nhỏ, chương hoặc phụ đề hay không.
- Có loại bỏ các đoạn SponsorBlock hay không.

Chọn **Tải xuống** để bắt đầu ngay lập tức. Cửa sổ tiến trình không chặn (modeless) của nó báo cáo dung lượng đã tải, phần trăm, tốc độ, thời gian ước tính và công việc hoàn thiện như ghép luồng, chuyển đổi hoặc nhúng thông tin. Bạn có thể tiếp tục sử dụng Luna trong khi cửa sổ này đang mở. Chọn **Thu nhỏ về hàng đợi** để ẩn cửa sổ tiến trình riêng lẻ và tiếp tục tải xuống trong hàng đợi.

Chọn **Thêm vào hàng đợi** để giữ lại mọi lựa chọn đã thực hiện trong hộp thoại làm cài đặt cho mục hàng đợi đó.

### Hàng đợi tải xuống

Nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> hoặc chọn **Công cụ > Hàng đợi tải xuống...** để mở hàng đợi. Danh sách hiển thị tiêu đề của từng video và trạng thái hiện tại của nó. Một liên kết được nhập thủ công sẽ hiển thị dưới dạng tiêu đề khi Luna chưa tra cứu siêu dữ liệu của nó.

- **Thêm...** chấp nhận một liên kết video YouTube trên mỗi dòng.
- **Bắt đầu** bắt đầu tất cả các mục trong hàng đợi và thử lại các mục bị lỗi.
- **Dừng** hủy quá trình tải xuống đang hoạt động đã chọn và đưa mục đó trở lại hàng đợi để bắt đầu sau.
- **Dừng tất cả** hủy tất cả các quá trình tải xuống đang hoạt động và để lại các mục chưa hoàn thành trong hàng đợi.
- Nhấn <kbd>Delete</kbd> để xóa mục đã chọn sau khi xác nhận. Xóa một mục đang hoạt động cũng sẽ hủy mục đó.
- **Đóng** ẩn cửa sổ hàng đợi mà không dừng các quá trình tải xuống đang hoạt động.

Hộp trạng thái và thanh tiến trình tổng thể chỉ xuất hiện khi hàng đợi chứa các mục. Trong khi một đợt tải đang chạy, chúng báo cáo mục hiện tại, phần trăm tổng thể, tốc độ kết hợp và thời gian còn lại ước tính. Một mục đã tải xuống có thể duy trì ở trạng thái hoàn thiện trong khi yt-dlp chuyển đổi, hợp nhất, nhúng hoặc di chuyển tệp đầu ra.

Hàng đợi chạy tối đa theo giới hạn **Số lượt tải xuống đồng thời** từ Tùy chọn YouTube. Các hàng đã hoàn thành vẫn có sẵn để xem lại cho đến khi cửa sổ hàng đợi đóng lại, sau đó chúng sẽ bị xóa. Luna gửi một thông báo trên màn hình khi tất cả các mục hiển thị trong hàng đợi hoàn thành thành công.

Nếu bạn đóng Luna trong khi hàng đợi vẫn còn các mục chưa hoàn thành, Luna sẽ hỏi bạn muốn thoát và hủy chúng hay quay lại ứng dụng.

### Video và luồng yêu thích

Danh sách Yêu thích lưu các liên kết, trong khi dấu trang lưu các vị trí bên trong các tệp cục bộ. Mở mục yêu thích bằng **Tệp > Video yêu thích...** hoặc <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd>.

Cửa sổ Yêu thích có thể mở, thêm, chỉnh sửa hoặc xóa các mục. Bạn có thể tiếp tục sử dụng trình phát chính trong khi cửa sổ này đang mở. Thêm hoặc chỉnh sửa một mục sẽ tạm thời vô hiệu hóa cửa sổ Yêu thích cho đến khi bạn hoàn tất hộp thoại đó, trong khi trình phát chính vẫn có thể sử dụng được. Mỗi mục có tên, liên kết và loại:

- **Video** yêu cầu liên kết video YouTube.
- **Danh sách phát** yêu cầu liên kết danh sách phát YouTube.
- **Liên kết kết hợp** yêu cầu địa chỉ chứa cả mã nhận dạng video và danh sách phát và sẽ mở video của nó.
- **Luồng chung** chấp nhận bất kỳ địa chỉ luồng `http` hoặc `https` nào.

Luna xác thực định dạng địa chỉ khi một mục được lưu. Ứng dụng chỉ kiểm tra xem nội dung từ xa có còn khả dụng hay không khi bạn mở nó.

### Các thành phần YouTube và cookie

Luna sử dụng yt-dlp và môi trường thực thi JavaScript để phát YouTube, duyệt kênh và tải xuống. Tìm kiếm, danh sách phát, mô tả và gợi ý tìm kiếm sử dụng máy khách siêu dữ liệu của Luna, nhưng một kết quả không thể phát được cho đến khi các thành phần được cài đặt. Sử dụng **Tải xuống các thành phần YouTube** trong Tùy chọn để cài đặt hoặc làm mới chúng.

Trang thành phần cung cấp ba kênh cập nhật:

- **Ổn định (Stable)** ít thay đổi nhất và được khuyến nghị cho nhu cầu sử dụng thông thường.
- **Nightly** cập nhật theo các bản dựng hàng đêm của yt-dlp.
- **Master** cập nhật theo mã nguồn mới nhất và có thể kém ổn định hơn.

Sử dụng **Trợ giúp > Cập nhật > Cập nhật các thành phần YouTube** để cập nhật yt-dlp đã cài đặt. Bật **Tìm bản cập nhật yt-dlp khi khởi động** để kiểm tra ở chế độ nền và chỉ nhắc khi có bản cập nhật mới.

Một số video bị giới hạn độ tuổi, chỉ dành cho hội viên hoặc được bảo vệ bằng xác minh yêu cầu cookie của trình duyệt. Bạn có thể chọn tệp cookie định dạng Netscape hoặc sử dụng **Nhập từ Firefox** để Luna đọc cookie Firefox hiện tại cho mỗi yêu cầu. Các lựa chọn này loại trừ lẫn nhau và trạng thái cookie chỉ đọc sẽ báo cáo nguồn nào đang hoạt động. Hãy coi các tệp cookie đã xuất như dữ liệu tài khoản riêng tư.

## 13. Spotify

Chọn **Công cụ > Spotify...** hoặc nhấn <kbd>Alt</kbd>+<kbd>S</kbd> để duyệt Spotify. Bạn có thể tìm kiếm bài hát, nghệ sĩ và danh sách phát, mở thư viện của mình và phát một bài hát đã chọn. **Việc duyệt Spotify trong Luna Player yêu cầu tài khoản Spotify Premium.** Bạn cũng cần có kết nối internet và sự ủy quyền từ Spotify.

### Định cấu hình ứng dụng Spotify của bạn

Các tính năng duyệt của Spotify yêu cầu Client ID từ ứng dụng nhà phát triển của riêng bạn. Nếu Client ID chưa được định cấu hình, việc mở Spotify sẽ hiển thị đề xuất **Đi đến Tùy chọn**.

1. Mở [Spotify Developer Dashboard](https://developer.spotify.com/dashboard) và đăng nhập.
2. Tạo một ứng dụng (Create app), nhập tên cùng mô tả và chọn **Web API**.
3. Thêm `http://127.0.0.1:43891/callback/` vào mục **Redirect URIs**. Sao chép chính xác địa chỉ này, bao gồm cổng và dấu gạch chéo cuối cùng.
4. Lưu ứng dụng và sao chép **Client ID**.
5. Trong Luna, mở **Tùy chọn > Spotify**, dán Client ID và chọn **OK**. Không dán Client Secret.

Spotify đặt ra các yêu cầu về tài khoản và quyền truy cập đối với các ứng dụng nhà phát triển. Các [yêu cầu chế độ phát triển](https://developer.spotify.com/documentation/web-api/concepts/quota-modes) hiện tại của Spotify yêu cầu chủ sở hữu ứng dụng phải có Spotify Premium. Nếu bạn sử dụng tài khoản khác để đăng nhập, tài khoản đó cũng phải được phép sử dụng ứng dụng trong bảng điều khiển. Một số danh sách phát và nội dung khác có thể không khả dụng đối với các ứng dụng ở chế độ phát triển.

### Đăng nhập và phát một bài hát

Mở trình duyệt Spotify và chọn **Đăng nhập vào Spotify**. Trình duyệt web mặc định của bạn sẽ mở trang ủy quyền của Spotify. Đăng nhập vào tài khoản bạn muốn duyệt, chấp thuận quyền truy cập và quay lại Luna khi trình duyệt xác nhận bạn có thể làm như vậy.

Trình duyệt Spotify có ba tab:

- **Trang chủ** chứa **Các bài hát hàng đầu của bạn**, **Đã phát gần đây** và các tuyển tập mix hoặc bộ sưu tập radio có sẵn từ thư viện của bạn. Đây không phải là bản sao của mọi đề xuất được hiển thị trong chính ứng dụng của Spotify.
- **Tìm kiếm** cho phép bạn nhập truy vấn và chọn **Tất cả**, **Bài hát**, **Nghệ sĩ** hoặc **Danh sách phát** trong mục **Lọc theo**. Kích hoạt **Tìm kiếm**, hoặc chọn một truy vấn trước đó từ **Tìm kiếm gần đây**.
- **Thư viện** liệt kê các danh sách phát của bạn và **Bài hát đã thích**. Chọn một bộ sưu tập và chọn **Mở mục đã chọn** để xem các bài hát của nó.

Trong kết quả tìm kiếm, chọn một bài hát để phát, hoặc mở một nghệ sĩ hoặc danh sách phát để duyệt các bài hát của họ. Trong danh sách bài hát, chọn một bài và chọn **Phát** hoặc nhấn <kbd>Enter</kbd>. Nhiều kết quả hoặc bài hát hơn sẽ được nạp khi bạn cuộn đến cuối danh sách. Nút **Quay lại** hoặc <kbd>Escape</kbd> sẽ quay trở về từ cửa sổ kết quả hoặc danh sách bài hát về trình duyệt.

Lần phát đầu tiên có thể mở lần đăng nhập trình duyệt thứ hai. Việc này ủy quyền riêng cho kết nối âm thanh tách biệt với kết nối Trang chủ, Tìm kiếm và Thư viện. Hoàn tất đăng nhập và quay lại Luna; Luna sẽ giữ lại ủy quyền đã lưu cho các phiên sau. Bạn không cần phải mở ứng dụng Spotify Desktop để phát bài hát đã chọn.

Việc chọn một bài hát sẽ đưa bạn trở lại trình phát chính. Các phím Trước đó và Tiếp theo sẽ di chuyển qua bộ sưu tập hoặc kết quả bài hát đã chọn, nạp thêm các bài hát tiếp theo khi khả dụng. Mở lại Spotify bằng <kbd>Alt</kbd>+<kbd>S</kbd> để chọn nội dung khác. Sử dụng **Làm mới** để nạp lại thư viện sau khi thay đổi thư viện trong Spotify. **Đăng xuất** sẽ ngắt kết nối tài khoản cùng ủy quyền phát lại và dừng phiên Spotify đang hoạt động.

### Chất lượng âm thanh và chuyển tiếp

Trong **Tùy chọn > Spotify > Chất lượng âm thanh phát luồng**, hãy chọn **Bình thường (96 kbps)**, **Cao (160 kbps)** hoặc **Rất cao (320 kbps)**. Rất cao là mặc định. Việc thay đổi chất lượng trong khi phát sẽ nạp lại bài hát hiện tại tại vị trí hiện tại của nó.

Spotify hỗ trợ các cài đặt phát liên tục không khoảng lặng (gapless) và hòa trộn chuyển tiếp (cross-fading) của Luna trong **Tùy chọn > Âm thanh > Phát lại**. Bật **Bật hòa trộn âm thanh chuyển tiếp (cross-fading) cho các tệp âm thanh** để chồng gối các bài hát và bật **Bật hòa trộn chuyển tiếp khi điều hướng thủ công** nếu bạn cũng muốn chồng gối âm thanh khi chọn Trước đó hoặc Tiếp theo. Bài hát tiếp theo phải sẵn sàng kịp thời; kết nối chậm hoặc bài hát không khả dụng có thể ngăn cản quá trình chuyển tiếp mượt mà. Xem mục [Hòa trộn chuyển tiếp](#hòa-trộn-chuyển-tiếp-cross-fading) để biết các cài đặt thời lượng và đường cong.

Các bộ sưu tập Spotify thuộc về Spotify. Việc duyệt chúng không sao chép chúng vào **Danh sách phát của tôi** và trình chỉnh sửa danh sách phát cục bộ của Luna không chỉnh sửa thư viện Spotify của bạn.

## 14. Trình duyệt radio

Chọn **Công cụ > Trình duyệt radio...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>R</kbd> để tìm các đài phát thanh internet thông qua [thư mục Radio Browser](https://www.radio-browser.info/) công khai. Bạn không cần phải có tài khoản.

### Tìm và phát một đài

Sử dụng cây **Thể loại** để chọn nội dung cần duyệt, sau đó chuyển sang danh sách **Đài phát thanh**. Các cột của danh sách hiển thị tên đài, quốc gia, ngôn ngữ, codec và tốc độ bit.

- **Đài phát thanh toàn cầu** tìm kiếm trên toàn bộ thư mục.
- **Yêu thích** chứa các đài bạn đã lưu trong Luna.
- **Đã phát gần đây** chứa lịch sử nghe cục bộ của bạn.
- **Khám phá** chứa các danh sách **Được phát nhiều nhất**, **Xu hướng**, **Được bình chọn nhiều nhất**, **Được nhấp gần đây** và **Cập nhật gần đây**. Các bảng xếp hạng này đề cập đến hoạt động trong thư mục công khai, không chỉ riêng lịch sử nghe của bạn.
- **Quốc gia** cho phép bạn duyệt theo quốc gia và khu vực. **Ngôn ngữ**, **Thể loại và thẻ**, và **Định dạng** cung cấp các cách khác để thu hẹp danh sách đài.

Mở rộng một danh mục để nạp các lựa chọn của nó. Để tìm kiếm, hãy nhập tên hoặc cụm từ vào ô **Tìm kiếm** và kích hoạt nút Tìm kiếm hoặc nhấn <kbd>Enter</kbd> trong ô đó. Chọn một danh mục đài, chẳng hạn như Đài phát thanh toàn cầu hoặc một ngôn ngữ cụ thể, trước khi tìm kiếm để thu hẹp các đài của danh mục đó. Sử dụng **Xóa tìm kiếm** để xóa truy vấn hiện tại. Các mục bổ sung sẽ được nạp khi bạn di chuyển về phía cuối danh sách.

Chọn một đài và chọn **Phát** hoặc nhấn <kbd>Enter</kbd> trong danh sách đài. Trình duyệt sẽ đóng và đài sẽ phát trong cửa sổ chính. Trước đó và Tiếp theo sẽ di chuyển qua danh sách đài bạn đã chọn, thay vì qua các tệp cục bộ không liên quan. Mở lại trình duyệt radio để chọn một danh mục hoặc đài khác.

Nhấn <kbd>I</kbd> trong cửa sổ chính để nghe tiêu đề chương trình phát sóng và <kbd>Shift</kbd>+<kbd>I</kbd> để nghe tên nghệ sĩ khi đài cung cấp thông tin đó. Một đài phát trực tiếp có thể không cung cấp thời lượng, tính năng tua hoặc thông tin bài hát đầy đủ. Tên đài và thông tin bài hát hiện tại tách biệt với nhau: một đài phát sóng có thể tiếp tục phát ngay cả khi không gửi tên nghệ sĩ hoặc tiêu đề.

### Lọc các đài phát thanh

Chọn **Bộ lọc…** để hạn chế kết quả. Trong mục **Áp dụng cho**, hãy chọn **Thể loại này** cho giới hạn cục bộ hoặc **Tất cả các thể loại** cho giới hạn toàn cục. Các giới hạn thể loại được kết hợp với các giới hạn toàn cục, vì vậy một đài phải thỏa mãn cả hai.

Bạn có thể lọc theo **Mã quốc gia** gồm hai chữ cái, **Khu vực**, **Ngôn ngữ**, **Các thẻ** phân tách bằng dấu phẩy, **Codec** và tốc độ bit tối thiểu hoặc tối đa. Tốc độ bit bằng 0 có nghĩa là không có giới hạn ở đầu dải đó. **Chỉ các đài đang hoạt động** ẩn các đài có lần kiểm tra thư mục gần nhất bị lỗi; tùy chọn này được bật theo mặc định. **Chỉ HTTPS** loại trừ các luồng HTTP thông thường. Chọn **Áp dụng** để sử dụng các giá trị hiển thị, **Đặt lại** để khôi phục các giá trị mặc định của bộ lọc hiển thị hoặc **Hủy** để giữ nguyên các giá trị đó.

Nếu một danh mục có vẻ trống, hãy xóa tìm kiếm của danh mục đó và kiểm tra cả bộ lọc danh mục cũng như bộ lọc cho tất cả các thể loại. Một đài được đánh dấu là đang hoạt động vẫn có thể tạm thời ngoại tuyến hoặc không khả dụng từ vị trí của bạn.

### Mục yêu thích, chi tiết và các thao tác đài phát thanh

Khi một đài được chọn, nhấn <kbd>Ctrl</kbd>+<kbd>Dấu cách</kbd> để thêm vào hoặc xóa khỏi mục Yêu thích. Bạn cũng có thể sử dụng menu ngữ cảnh của danh sách đài. Các mục yêu thích này được lưu giữ trong thư viện radio của Luna, tách biệt với **Tệp > Video yêu thích...**.

Chọn **Chi tiết đài phát thanh** hoặc nhấn <kbd>Shift</kbd>+<kbd>Enter</kbd> để đọc thông tin của thư mục về đài đó. Menu ngữ cảnh cũng cung cấp **Mở trang web**, **Sao chép URL luồng**, **Sao chép tên đài phát thanh** và **Bình chọn cho đài phát thanh**. Trang web và URL luồng là khác nhau: trang web là trang chủ của đài, trong khi URL luồng trỏ đến âm thanh của đài.

Sử dụng **Làm mới** để nạp lại chế độ xem hiện tại. **Xóa lịch sử…** sẽ hỏi trước khi xóa lịch sử Đã phát gần đây cục bộ của bạn; thao tác này không xóa mục yêu thích hoặc thay đổi thư mục công khai.

## 15. Ghi âm âm thanh

Luna có thể ghi âm micrô và các đầu vào khác, mọi thứ phát qua một thiết bị đầu ra, hoặc âm thanh của một chương trình cụ thể. Nhiều nguồn có thể được hòa trộn vào một bản ghi duy nhất với các mức âm lượng riêng biệt.

### Ghi âm nhanh với micrô mặc định

Nếu chưa có nguồn nào được tạo trong phiên hiện tại, hãy nhấn <kbd>F9</kbd> để ghi âm thiết bị đầu vào mặc định của Windows bằng các Tùy chọn Ghi âm đã lưu. Nhấn <kbd>F7</kbd> để tạm dừng hoặc tiếp tục, và <kbd>F8</kbd> để dừng.

Âm báo tăng dần xác nhận quá trình ghi âm đang bắt đầu. Âm báo giảm dần xác nhận quá trình ghi âm đã dừng. Việc bắt đầu và dừng thành công không đọc thêm thông báo giọng nói đè lên các âm báo đó.

### Mở giao diện ghi âm

Chọn **Ghi âm > Mở giao diện ghi âm...** hoặc nhấn <kbd>Alt</kbd>+<kbd>R</kbd>. Cửa sổ có danh sách nguồn, điều khiển nguồn, cài đặt đầu ra và điều khiển Bắt đầu/Tạm dừng. Cửa sổ này vẫn cho phép sử dụng trình phát chính. Hộp thoại nguồn và đầu ra thuộc về cửa sổ ghi âm và không chặn các điều khiển phát lại trong cửa sổ chính.

Đóng cửa sổ này không làm dừng bản ghi âm đang hoạt động. Tính năng ghi âm thuộc về ứng dụng và tiếp tục cho đến khi bạn sử dụng lệnh Dừng, <kbd>F8</kbd>, hoặc thoát Luna.

### Thêm các nguồn

Kích hoạt **Thêm** và chọn:

- **Thiết bị đầu vào...** cho micrô, đầu vào line-in hoặc thiết bị thu âm khác của Windows.
- **Đầu ra hệ thống...** cho mọi thứ đang phát qua một loa hoặc thiết bị tai nghe đã chọn.
- **Chương trình...** cho âm thanh của một chương trình cụ thể. Tính năng này yêu cầu Windows 10 phiên bản 2004 trở lên.

Đặt tên mang tính mô tả cho nguồn, chọn thiết bị hoặc chương trình, và đặt âm lượng của nguồn đó. Một nguồn thiết bị có thể tuân theo thiết bị mặc định hiện tại của Windows thay vì một thiết bị có tên cố định.

Đối với nguồn chương trình, tùy chọn **Thu mọi thứ ngoại trừ ứng dụng này** sẽ đảo ngược lựa chọn và thu âm thanh của các ứng dụng khác thay thế. Chỉ các chương trình có phiên âm thanh Windows đang hoạt động mới xuất hiện trong danh sách; hãy bật âm thanh trong chương trình đích nếu chương trình đó chưa xuất hiện.

Sử dụng **Chỉnh sửa...** để thay đổi nguồn đã chọn và **Xóa...** để xóa nguồn đó khỏi phiên. Thanh trượt âm lượng nguồn thay đổi mức độ của nguồn đó trong bản hòa âm cuối cùng.

Các nguồn chỉ có hiệu lực theo phiên. Chúng vẫn được giữ lại khi cửa sổ ghi âm được đóng và mở lại trong cùng một phiên Luna, nhưng không được khôi phục sau khi thoát Luna vì các thiết bị và mã định danh tiến trình có thể không còn đại diện cho cùng một nguồn.

### Định dạng và thư mục lưu bản ghi âm

Chọn định dạng, tốc độ lấy mẫu, số kênh, chất lượng khi áp dụng và thư mục đích:

- **WAV** không nén và thường tạo ra tệp có dung lượng lớn nhất.
- **FLAC** không nén mất dữ liệu (lossless) và thường nhỏ hơn WAV.
- **MP3** và **AAC** nén mất dữ liệu (lossy) và sử dụng tốc độ bit đã chọn để cân bằng giữa dung lượng và chất lượng.
- **Mono** thường đủ dùng cho một micrô hoặc giọng nói.
- **Stereo** bảo toàn các kênh trái và phải riêng biệt cho âm nhạc và đầu ra hệ thống.

Các tốc độ lấy mẫu, số lượng kênh và tốc độ bit khả dụng được lấy từ các bộ mã hóa được cài đặt cùng với Windows. Do đó, các lựa chọn sẽ thay đổi theo định dạng đã chọn và có thể khác nhau tùy theo từng hệ thống. Luna đặt cho mỗi bản ghi một cái tên có mốc thời gian để tránh ghi đè lên tệp trước đó.

Các thay đổi được thực hiện bên trong giao diện ghi âm áp dụng cho phiên của nó. Để thay đổi các giá trị mặc định được sử dụng sau lần khởi chạy tiếp theo hoặc bằng các phím tắt trực tiếp, hãy sử dụng **Tùy chọn > Ghi âm**.

### Bắt đầu, tạm dừng và dừng

Cửa sổ ghi âm yêu cầu ít nhất một nguồn được định cấu hình trước khi nút **Bắt đầu** khả dụng. Khi quá trình ghi bắt đầu, các điều khiển nguồn và đầu ra sẽ bị khóa để định dạng bản ghi và bản hòa âm luôn nhất quán.

- Nút **Bắt đầu** trở thành nút **Dừng** trong khi ghi âm.
- Nút **Tạm dừng** trở thành nút **Tiếp tục** khi đang tạm dừng.
- <kbd>F9</kbd>, <kbd>F7</kbd> và <kbd>F8</kbd> hoạt động từ cửa sổ chính của Luna.
- Các phím tắt toàn cục tương ứng hoạt động ngay cả khi một ứng dụng khác đang giữ tiêu điểm.

Nếu chỉ có một số nguồn mở được, Luna sẽ bắt đầu với các nguồn khả dụng và liệt kê những nguồn bị lỗi. Nếu không có nguồn nào mở được, quá trình ghi âm sẽ không bắt đầu.

Chọn **Ghi âm > Mở thư mục ghi âm** để mở thư mục đích đang hoạt động. Thư mục mặc định là `Documents\Luna Player\Recordings`.

## 16. IPTV

Các nguồn và kênh IPTV nằm trong menu **Công cụ > IPTV**. Luna hỗ trợ danh sách phát M3U hoặc M3U8 mở rộng từ tệp hoặc địa chỉ web, tài khoản Xtream Codes và cổng Stalker portal.

### Quản lý các nguồn

Chọn **Công cụ > IPTV > Quản lý nguồn IPTV...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>I</kbd>. Danh sách nguồn cung cấp các lệnh **Mở**, **Thêm...**, **Chỉnh sửa...** và **Xóa...**.

Mỗi nguồn có một tên và một loại. Tùy thuộc vào loại đó, Luna sẽ yêu cầu đường dẫn hoặc địa chỉ danh sách phát, máy chủ Xtream với tên người dùng và mật khẩu, hoặc cổng Stalker với địa chỉ MAC và số sê-ri tùy chọn. Bạn cũng có thể cung cấp địa chỉ hướng dẫn chương trình XMLTV tùy chọn. Các kênh trực tiếp Xtream có thể được mở dưới dạng HLS thay vì luồng truyền tải (transport stream) khi tùy chọn **Mở kênh trực tiếp dạng HLS** được bật.

Thông tin xác thực IPTV đã lưu được bảo vệ cho người dùng Windows hiện tại. Không chia sẻ tệp dữ liệu nguồn IPTV vì tệp này thuộc về tài khoản và máy tính của bạn.

### Duyệt kênh

Mở một nguồn từ trình quản lý, hoặc chọn **Công cụ > IPTV > Duyệt kênh...** để mở lại nguồn hiện tại. Trình duyệt kênh có thể lọc theo truyền hình trực tiếp (live TV), phim (movies) hoặc phim bộ (series), theo danh mục, và theo văn bản được nhập vào ô Tìm kiếm.

Nhấn <kbd>Enter</kbd> trên một hàng đã chọn hoặc kích hoạt **Phát** để mở kênh đó. Khi có thông tin hướng dẫn chương trình, nhấn <kbd>Ctrl</kbd>+<kbd>I</kbd> để nghe những gì đang phát hiện tại và những gì phát tiếp theo. Nhấn <kbd>F1</kbd> trong trình duyệt kênh để nghe bản tóm tắt bằng giọng nói về các điều khiển của nó.

## 17. Bộ chuyển đổi phương tiện

Chọn **Công cụ > Chuyển đổi phương tiện...** để chuyển đổi một hoặc nhiều tệp phương tiện sang định dạng âm thanh. Thêm các tệp riêng lẻ hoặc thêm một thư mục; thêm một thư mục sẽ bao gồm các phương tiện được hỗ trợ trong các thư mục con của nó. Các đường dẫn trùng lặp sẽ bị bỏ qua.

Bộ chuyển đổi hỗ trợ xuất ra các định dạng AAC, AC-3, AIFF, FLAC, M4A, MKA, MP3, OGA, OGG, Opus, WAV và WMA. Công cụ sẽ tự động chọn định dạng ban đầu hợp lý từ tệp nguồn đầu tiên, nhưng bạn có thể thay đổi định dạng đó. Tùy thuộc vào định dạng đầu ra, bạn cũng có thể chọn tốc độ lấy mẫu, kênh mono hoặc stereo, tốc độ bit, cùng chế độ và cấu hình tốc độ bit Opus.

Đặt **Số lượt chuyển đổi đồng thời tối đa** để kiểm soát số lượng tệp chạy cùng một lúc. Theo mặc định, các tệp đã chuyển đổi được ghi cạnh các tệp nguồn của chúng. Bỏ chọn **Giữ nguyên vị trí tệp gốc** để gửi tất cả kết quả vào một thư mục đích duy nhất.

Tùy chọn **Xóa tệp gốc sau khi chuyển đổi** chỉ xóa từng tệp nguồn sau khi tệp đó chuyển đổi thành công. Hãy cẩn thận khi sử dụng tùy chọn này; các tệp gốc đã xóa sẽ không được chuyển vào Thùng rác.

Cửa sổ tiến trình báo cáo tệp hiện tại, phần trăm của tệp, số lượng tệp còn lại, tổng phần trăm và thời gian ước tính. Bạn có thể hủy công việc còn lại bất kỳ lúc nào. Khi quá trình chuyển đổi kết thúc, Luna sẽ báo cáo thành công hoàn toàn hoặc liệt kê các tệp bị lỗi cùng lý do.

## 18. Hẹn giờ ngủ

Chọn **Công cụ > Hẹn giờ ngủ > Đặt hẹn giờ...** hoặc nhấn <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>S</kbd>. Bộ hẹn giờ có thể kích hoạt sau một khoảng thời gian đã chọn hoặc khi kết thúc bài hát hiện tại. Chọn xem ứng dụng sẽ tạm dừng phát lại hay dừng và hủy nạp mục hiện tại.

Đối với khoảng thời gian, hãy chọn 15, 30, 45, 60, 90 hoặc 120 phút, hoặc nhập số giờ và số phút tùy chỉnh. Hẹn giờ kết thúc bài hát yêu cầu một mục đang mở và ngăn chặn việc tự động chuyển bài thông thường khi mục đó kết thúc.

Bật **Giảm dần âm lượng trước khi kích hoạt** để giảm âm lượng trong khoảng từ 1 đến 120 giây cuối cùng. Luna sẽ khôi phục lại âm lượng ban đầu sau khi bộ hẹn giờ kích hoạt hoặc bị hủy; giá trị âm lượng đã giảm dần không được lưu làm âm lượng ưu tiên của bạn.

- Nhấn <kbd>Ctrl</kbd>+<kbd>T</kbd> hoặc chọn **Thời gian còn lại** để nghe thời gian còn lại hoặc biết bộ hẹn giờ đang đợi kết thúc bài hát.
- Chọn **Hủy hẹn giờ** để tắt bộ hẹn giờ. Mở cửa sổ hẹn giờ trong khi đang có hẹn giờ hoạt động cũng cung cấp tùy chọn **Tắt hẹn giờ**.
- Luna sẽ đọc thông báo cảnh báo trước một phút đối với các bộ hẹn giờ theo thời lượng.

Các lựa chọn hẹn giờ gần nhất sẽ được ghi nhớ, nhưng quá trình đếm ngược đang hoạt động sẽ không duy trì sau khi đóng Luna.

## 19. Tham chiếu cài đặt tùy chọn

Mở Tùy chọn bằng **Tệp > Tùy chọn...** hoặc <kbd>Ctrl</kbd>+<kbd>P</kbd>. Chọn một trang trong cây danh mục. Chọn OK để lưu và áp dụng các cài đặt bạn đã thay đổi, hoặc Hủy để hủy bỏ các thay đổi chưa được chấp nhận. Các cài đặt không thay đổi sẽ không được áp dụng lại, vì vậy việc lưu một tùy chọn không liên quan sẽ không đặt lại các điều chỉnh phát lại tạm thời như âm thanh không gian cho tai nghe.

Nhấn <kbd>F1</kbd> trong khi một cài đặt đang giữ tiêu điểm để nghe giải thích chi tiết về cài đặt đó.

### Chung

| Cài đặt | Mục đích |
| --- | --- |
| Ngôn ngữ | Sử dụng ngôn ngữ hiển thị của Windows hoặc bản dịch đi kèm. Khởi động lại Luna sau khi thay đổi. |
| Giao diện | Tuân theo Windows hoặc buộc sử dụng giao diện sáng hoặc tối. Tương phản cao luôn tuân theo Windows. Khởi động lại Luna sau khi thay đổi. |
| Khởi động ở chế độ phóng to tối đa | Mở cửa sổ chính ở chế độ phóng to tối đa vào lần khởi chạy tiếp theo. Mặc định tắt, giữ kích thước cửa sổ bình thường. |
| Ghi nhớ vị trí tệp lần trước | Khôi phục tệp cục bộ đang hoạt động gần nhất và vị trí của tệp vào lần khởi chạy tiếp theo. |
| Đọc tên tệp khi điều hướng | Đọc thông báo mục vừa chuyển đến khi sử dụng Trước đó hoặc Tiếp theo. |
| Kiểm tra cập nhật ứng dụng khi khởi động | Kiểm tra âm thầm sau khi khởi động và chỉ nhắc khi có bản phát hành mới hơn sẵn sàng. |
| Lưu cài đặt khi đóng | Lưu các thay đổi trong phiên như âm lượng và tốc độ khi Luna thoát. Các thay đổi Tùy chọn được chấp nhận rõ ràng vẫn được lưu ngay lập tức. |
| Mở các tệp trong cửa sổ hiện có khi Luna đang chạy | Gửi các tệp được mở từ Windows đến cửa sổ Luna hiện tại. Bỏ chọn để mở mỗi yêu cầu trong một cửa sổ riêng biệt. |
| Vô hiệu hóa điều khiển đa phương tiện hệ thống | Xóa Luna khỏi lớp phủ đa phương tiện của Windows và bỏ qua các phím đa phương tiện phần cứng. |
| Hiển thị bài hát đang phát trên tiêu đề cửa sổ | Hiển thị mục hiện tại và trạng thái tạm dừng trên thanh tiêu đề của Luna. Tùy chọn này mặc định tắt để tránh các thông báo lặp đi lặp lại của trình đọc màn hình. |
| Ngắt dòng hiện tại khi xuất hiện phụ đề mới | Cho phép phụ đề đọc bằng giọng nói mới cắt ngang phụ đề trước đó để theo kịp hình ảnh. |
| Mức độ chi tiết giọng đọc | Chọn thông báo đầy đủ cho Người mới bắt đầu hoặc thông báo ngắn gọn Nâng cao. |
| Bạn muốn mở gì cùng với các tệp? | Kiểm soát xem một tệp được mở có nạp thêm các tệp cùng thư mục, hoặc thư mục và các thư mục con hay không. |
| Đăng ký/Hủy đăng ký phần mở rộng tệp | Thêm hoặc xóa các đăng ký phương tiện được hỗ trợ của Luna cho người dùng Windows hiện tại. |

### Sao lưu và khôi phục

| Lệnh | Mục đích |
| --- | --- |
| Xuất cài đặt | Lưu các tùy chọn hiện đang áp dụng vào tệp JSON mà không làm thay đổi bản sao đang hoạt động. |
| Nhập cài đặt | Nạp tệp JSON cài đặt hợp lệ và thay thế các giá trị hiển thị trong Tùy chọn. |
| Xuất dấu trang | Lưu tất cả các dấu trang vào một tệp JSON. |
| Nhập dấu trang | Thay thế bộ sưu tập dấu trang hiện tại bằng bộ sưu tập đã xuất hợp lệ. |
| Đặt lại cài đặt | Khôi phục mọi tùy chọn và phím tắt về mặc định sau khi xác nhận. |
| Mở thư mục cài đặt người dùng | Mở thư mục cấu hình của Luna trong File Explorer. |

Nhập cài đặt sẽ thay thế bộ sưu tập tương ứng; nó không hợp nhất các mục. Hãy giữ các tệp đã xuất ở một nơi tách biệt với thư mục cấu hình của Luna nếu bạn dự định dùng chúng làm bản sao lưu.

### Âm thanh

| Cài đặt | Mục đích |
| --- | --- |
| Giá trị tua tùy chỉnh | Số giây được sử dụng bởi khoảng cách tua tùy chỉnh; chấp nhận số thập phân dương như 2.5. |
| Bước tốc độ | Mức tăng hoặc giảm của mỗi lệnh thay đổi tốc độ. |
| Bước cao độ | Mức thay đổi cao độ mỗi lần nhấn, từ 0.001 đến 12 nửa cung. |
| Bước âm lượng | Mức thay đổi âm lượng mỗi lần nhấn, từ 1 đến 20 điểm phần trăm. |
| Bước cân bằng | Mức di chuyển trái/phải mỗi lần nhấn, từ 1 đến 100 điểm phần trăm. |
| Điều gì xảy ra khi tệp kết thúc? | Chuyển tiếp sang tệp tiếp theo, lặp lại tệp, hoặc dừng lại ở cuối tệp. |
| Quay lại đầu khi mở nhiều tệp | Kết nối phần cuối và phần đầu của danh sách nhiều mục. |
| Lưu vị trí hiện tại cho từng tệp | Duy trì vị trí tiếp tục riêng biệt cho từng mục cục bộ. |
| Bật chuẩn hóa động và giới hạn âm lượng | Làm đồng đều độ lớn và kiềm chế các đỉnh âm thanh. |
| Phát âm thanh dạng Mono | Hòa trộn kênh trái và phải thành âm thanh mono. |

#### Các giá trị mặc định của âm thanh không gian cho tai nghe

Trang này chọn bộ dữ liệu SOFA và trạng thái bật mặc định, độ khuếch đại, góc xoay, góc nâng và các bước định hướng. Luna không bao gồm sẵn bộ dữ liệu. Xem mục [Âm thanh không gian cho tai nghe](#âm-thanh-không-gian-cho-tai-nghe-headphone-spatialization) trong phần xử lý âm thanh để biết các liên kết tải xuống trực tiếp, hướng dẫn thiết lập và phím tắt phát lại.

#### Làm mờ âm thanh và phát liên tục (Fades and gapless playback)

Dưới mục **Âm thanh > Phát lại**, các tùy chọn làm mờ âm lượng kiểm soát phần đầu và phần cuối của tệp, việc tạm dừng và tiếp tục phát, cũng như thao tác tua. **Khoảng thời gian làm mờ âm lượng (mili-giây)** đặt thời lượng làm mờ thông thường; đặt bằng 0 sẽ tắt các hiệu ứng làm mờ này. **Bật làm mờ âm lượng khi tua** sử dụng **Khoảng thời gian làm mờ khi tua (mili-giây)** riêng biệt, từ 1 đến 10000. Làm mờ sẽ giảm nhỏ âm thanh xung quanh một thao tác; nó không chồng gối hai bản nhạc lên nhau.

**Chế độ phát liên tục không khoảng lặng (Gapless playback mode)** kiểm soát xem Luna có giữ mở đầu ra âm thanh giữa các bản nhạc hay không. **Đã tắt** sử dụng cách chuyển tệp thông thường. **Các tệp tương thích** bảo toàn định dạng âm thanh gốc và nối các bản nhạc khi định dạng của chúng tương thích. **Luôn luôn khi có thể** có thể điều chỉnh các bản nhạc sau theo định dạng đầu ra của bản nhạc đầu tiên để quá trình phát có thể tiếp tục qua đầu ra đó. Chọn Các tệp tương thích nếu bạn muốn giữ nguyên định dạng của từng bản nhạc. Phát trực tuyến cũng yêu cầu luồng tiếp theo phải sẵn sàng kịp thời. Xem mục [Spotify](#13-spotify) để phát từ một bộ sưu tập Spotify.

#### Hòa trộn chuyển tiếp (Cross-fading)

Dưới mục **Âm thanh > Phát lại**, hãy chọn **Bật hòa trộn âm thanh chuyển tiếp (cross-fading) cho các tệp âm thanh** để chồng gối phần cuối của một tệp âm thanh với phần đầu của tệp tiếp theo. Hòa trộn chuyển tiếp mặc định tắt và loại trừ các tệp chứa video. Ảnh bìa album không được tính là video.

Đặt **Thời lượng hòa trộn chuyển tiếp (mili-giây)** từ 1 đến 30000; mặc định là 3000 (tức ba giây). Chọn **Công suất không đổi (Equal-power)** để có độ lớn cảm nhận đồng đều trong suốt khoảng thời gian chồng gối, hoặc **Tuyến tính (Linear)** để thay đổi độ khuếch đại ở tốc độ không đổi. Công suất không đổi có thể tạo ra các đỉnh âm thanh cao hơn.

Chọn **Bật hòa trộn chuyển tiếp khi điều hướng thủ công** để sử dụng tính năng chồng gối khi bạn tự di chuyển giữa các tệp âm thanh, bao gồm các lệnh Trước đó, Tiếp theo và số thứ tự tệp. Tùy chọn này mặc định tắt. Điều hướng trong khi tạm dừng sử dụng cách chuyển tệp thông thường. Các cài đặt hòa trộn chuyển tiếp này cũng áp dụng cho các bài hát Spotify.

Hòa trộn chuyển tiếp hoạt động độc lập với cài đặt phát liên tục và được ưu tiên hơn tính năng làm mờ chuyển tiếp tệp thông thường. Khi một bản hòa trộn chuyển tiếp kết thúc, vị trí phát của bản nhạc tiếp theo sẽ phản ánh khoảng thời gian chồng gối đó. Ví dụ: sau một bản hòa trộn chuyển tiếp ba giây, bản nhạc tiếp theo bắt đầu ở giây thứ 3.

### Loại bỏ khoảng lặng

Trang này cấu hình bộ lọc loại bỏ khoảng lặng được sử dụng bởi lệnh **Trình phát > Bật bộ lọc loại bỏ khoảng lặng** hoặc phím tắt <kbd>Ctrl</kbd>+<kbd>M</kbd>. Xem [Loại bỏ khoảng lặng và xử lý âm thanh](#8-loại-bỏ-khoảng-lặng-và-xử-lý-âm-thanh) để biết giải thích về từng trường cơ bản và nâng cao.

### YouTube

Trang này thiết lập các giá trị mặc định cho chất lượng phát lại, hành vi liên kết kết hợp, chất lượng tải xuống, định dạng và các tùy chọn hoàn thiện. Xem [YouTube](#12-youtube) để biết các tùy chọn tải xuống và hướng dẫn về các thành phần và cookie của YouTube.

### Spotify

Trang này quản lý Client ID của ứng dụng nhà phát triển Spotify của bạn và chất lượng âm thanh phát luồng. Xem [Spotify](#13-spotify) để biết các bước thiết lập bảng điều khiển nhà phát triển và các chi tiết về tài khoản.

### Discord Rich Presence

Luna có thể chia sẻ thông tin phát lại hiện tại với ứng dụng Discord Desktop đang chạy cục bộ trên máy tính. Tính năng này sử dụng Discord Rich Presence (hoạt động hiện tại của bạn) và mặc định bị tắt. Ứng dụng không yêu cầu bot hay mã thông báo người dùng; Luna kết nối trực tiếp với Discord trên cùng một máy tính.

#### Bật chia sẻ hoạt động

1. Mở Discord Desktop, đi tới **Cài đặt người dùng > Quyền riêng tư về hoạt động** và bật **Chia sẻ hoạt động đã phát hiện của bạn với người khác**.
2. Trong Luna, mở **Tùy chọn > Discord**.
3. Chọn **Bật Discord Rich Presence** và chọn **OK**.

Luna sẽ kết nối với Discord sau vài giây. Khi quá trình phát tiếp tục, thẻ hoạt động của Luna sẽ xuất hiện trên hồ sơ Discord của bạn.

#### Những gì xuất hiện trên hồ sơ của bạn

Luna điều chỉnh sự hiện diện của mình theo loại nội dung phương tiện:

- **Âm thanh cục bộ:** Thẻ Đang nghe nhạc (Listening) hiển thị tiêu đề tệp hoặc siêu dữ liệu và thời gian trôi qua.
- **Video cục bộ:** Thẻ Đang xem (Watching) hiển thị tiêu đề và tiến trình thời gian.
- **Sách nói:** Thẻ hiển thị tiêu đề cuốn sách, chương hiện tại và thời gian của chương đó.
- **YouTube:** Thẻ hiển thị tiêu đề video, tên kênh và hình thu nhỏ của video từ YouTube, kèm theo nút **Xem trên YouTube** hoặc **Nghe trên YouTube**.
- **Spotify:** Thẻ hiển thị bài hát, nghệ sĩ, album và ảnh bìa nghệ thuật từ Spotify, kèm theo nút **Mở trong Spotify**.
- **Radio:** Thẻ hiển thị tên đài phát thanh và tiêu đề chương trình phát sóng (nếu có), kèm theo nút **Xem trực tuyến** mở luồng trong trình duyệt.

Đối với phương tiện có thời lượng xác định, Luna cung cấp mốc thời gian tiến trình sử dụng cùng thời gian đã phát và tổng thời gian phương tiện như trong trình phát, mà không chia cho tốc độ phát lại. Thẻ Listening của Discord hiển thị thời gian đã trôi qua và tổng thời gian, kể cả khi chế độ sách nói được bật. Phương tiện tạm dừng vẫn hiển thị với nhãn **Tạm dừng** và không có bộ đếm thời gian đang chạy. Tiếp tục phát, tua, thay đổi tốc độ hoặc chuyển sang chương khác sẽ cập nhật bộ đếm thời gian. Các luồng phát trực tiếp hiển thị thời gian đã trôi qua khi không biết trước thời lượng tổng thể.

#### Các điều khiển chia sẻ

| Cài đặt | Mục đích |
| --- | --- |
| Bật Discord Rich Presence | Bật hoặc tắt tính năng chia sẻ hoạt động. |
| Chia sẻ tiêu đề phương tiện, nghệ sĩ và ảnh bìa | Bao gồm tiêu đề, thông tin nghệ sĩ, chương sách và ảnh bìa trực tuyến. Bỏ chọn để chỉ hiển thị trạng thái phát chung và ẩn các liên kết nguồn. |
| Hiển thị các nút liên kết | Thêm nút **Tải xuống Luna Player** và nút nguồn cho YouTube, Spotify hoặc luồng radio công khai khi chi tiết phương tiện được chia sẻ. |
| Hiển thị trạng thái khi dừng phát hoặc không hoạt động | Hiển thị **Nghỉ (Idle)** khi dừng phát hoặc không có phương tiện nào được nạp. Bỏ chọn để xóa hoạt động khi không hoạt động. Tạm dừng không được tính là nghỉ. |

Đối với radio, **Xem trực tuyến** sẽ mở chính luồng đó trong trình duyệt của người xem. Một số trình duyệt có thể tải về hoặc từ chối luồng mà chúng không thể phát. Luna bỏ qua các địa chỉ riêng tư, các liên kết chứa thông tin xác thực và các liên kết có tham số truy vấn không nhận dạng được. Đường dẫn tệp cục bộ không bao giờ được dùng làm nút bấm. Bỏ chọn **Hiển thị các nút liên kết** sẽ xóa cả hai nút mà không ẩn ảnh bìa.

#### Nếu hoạt động không xuất hiện

Hãy kiểm tra xem cả tính năng chia sẻ hoạt động của Discord và tùy chọn Rich Presence của Luna đều đã được bật. Luna lấy mẫu phát lại mỗi giây một lần và gửi các thay đổi trạng thái mà không có độ trễ cố định bốn giây. Các thay đổi nhanh chóng sẽ được kết hợp lại khi đạt đến giới hạn gửi của Discord. Luna sẽ tự động kết nối lại khi Discord khởi động hoặc khởi động lại; vui lòng đợi khoảng 15 giây. Nếu Discord không khả dụng, quá trình phát lại vẫn tiếp tục bình thường.

Discord có thể ẩn các nút hoạt động của chính bạn khi bạn tự xem hồ sơ của mình. Người khác khi xem hồ sơ của bạn vẫn có thể nhìn thấy các nút đó.

### Ghi âm

Trang này thiết lập các giá trị mặc định được sử dụng bởi các phím tắt ghi âm trực tiếp và khởi tạo giao diện ghi âm khi bắt đầu mỗi phiên Luna. Trang bao gồm định dạng âm thanh, tốc độ lấy mẫu, số kênh, chất lượng âm thanh cho các định dạng nén, và thư mục lưu các bản ghi âm.

### Phím tắt bàn phím

Trang này liệt kê mọi hành động có thể thực hiện trong khi cửa sổ chính của Luna đang giữ tiêu điểm. Mỗi hành động có thể có một phím tắt chính; một vài hành động cũng có thêm một ô phím tắt phụ được xác định trước.

1. Chọn một hành động.
2. Kích hoạt **Chỉnh sửa phím tắt chính** hoặc **Chỉnh sửa phím tắt phụ** (nếu có).
3. Nhấn tổ hợp phím mong muốn. Nhấn <kbd>Escape</kbd> để hủy chụp phím.
4. Nhấn OK trong Tùy chọn để áp dụng các thay đổi.

Ngay bên dưới danh sách phím tắt, tùy chọn **Tắt các phím tắt phụ** sẽ tắt mọi gán phím trong cột Phím tắt phụ. Khi đó các phím sẽ giữ nguyên hành vi thông thường trong giao diện; ví dụ: Tab và Shift+Tab có thể di chuyển tiêu điểm thay vì chuyển bài hát. Phím tắt chính và phím tắt toàn cục không bị ảnh hưởng. Các gán phím tắt phụ của bạn vẫn được giữ nguyên và sẽ hoạt động trở lại khi bạn bỏ chọn hộp kiểm và chọn OK. Page Up và Page Down là các phím tắt chính cho Trước đó và Tiếp theo.

Luna sẽ từ chối tổ hợp phím đã được gán cho một hành động khác trong cùng một bộ phím tắt. Phím tắt cục bộ có thể sử dụng các phím ký tự in được, phím điều hướng, phím chức năng từ F1 đến F24 và các phím bổ trợ Ctrl, Shift hoặc Alt. Sử dụng **Đặt lại về mặc định** để khôi phục toàn bộ trang phím tắt cục bộ về ban đầu sau khi xác nhận.

### Phím tắt toàn cục

Phím tắt toàn cục hoạt động ngay cả khi một ứng dụng khác đang giữ tiêu điểm, bất kể Luna đang thu nhỏ hay chỉ nằm phía sau một cửa sổ khác sau khi Alt+Tab. Chúng không chạy trong khi Luna hoặc một trong các hộp thoại của nó đang giữ tiêu điểm; hãy sử dụng các phím tắt cục bộ tại đó. Các lệnh toàn cục được giới hạn ở các hành động phát lại, điều hướng, âm lượng và ghi âm hữu ích bên ngoài Luna.

Chọn một hành động, kích hoạt **Chỉnh sửa phím tắt**, và nhấn tổ hợp phím mong muốn. Phím tắt toàn cục cũng có thể sử dụng phím Windows. Luna chuyển các tổ hợp phím này tới Windows, vì vậy một phím tắt của hệ điều hành có cùng các phím đó vẫn có thể thực hiện hành động riêng của nó. Hãy chọn một tổ hợp phím khác nếu điều này gây ra tác dụng phụ không mong muốn.

Phím tắt toàn cục tách biệt với phím tắt cục bộ. Thay đổi một bên sẽ không làm thay đổi bên còn lại.

## 20. Cập nhật ứng dụng

### Kiểm tra thủ công

Chọn **Trợ giúp > Cập nhật > Kiểm tra cập nhật ứng dụng**. Một hộp thoại tiến trình có thể hủy sẽ xuất hiện trong khi Luna kiểm tra thông tin bản phát hành.

Nếu có bản phát hành mới hơn, Luna sẽ hiển thị phiên bản đã cài đặt và phiên bản hiện có cùng danh sách các thay đổi có thể cuộn được. Chọn **Cập nhật** để tải xuống hoặc **Để sau** để giữ nguyên phiên bản hiện tại.

Luna xác minh rằng trình cài đặt hoặc tệp nén di động thực tế có tồn tại trên máy chủ trước khi đề xuất cập nhật. Nếu thông tin bản phát hành được công bố trước khi gói của nó hoàn tất tải lên, bước kiểm tra thủ công sẽ yêu cầu bạn thử lại sau thay vì hiển thị một liên kết tải xuống sẽ bị lỗi.

### Kiểm tra khi khởi động

Bật **Kiểm tra cập nhật ứng dụng khi khởi động** để ứng dụng kiểm tra âm thầm trong nền. Ứng dụng không hiển thị cửa sổ tiến trình hoặc thông báo không có cập nhật; hộp thoại nhắc nhở chỉ xuất hiện khi có gói tương thích mới hơn sẵn sàng.

### Cài đặt bản cập nhật đã tải xuống

Cửa sổ tải xuống hiển thị tổng dung lượng, dung lượng đã tải, phần trăm và nút Hủy. Cửa sổ này vẫn cho phép sử dụng trình phát chính, vì vậy bạn có thể tạm dừng, dừng hoặc thay đổi nội dung phát lại trong khi chờ đợi. Sau khi tải xuống thành công, Luna sẽ khởi chạy trình hỗ trợ cập nhật nhỏ và đóng lại:

- Bản cài đặt sẽ khởi chạy trình cài đặt bản cập nhật. Tiến trình cài đặt của nó vẫn hiển thị và Luna sẽ mở lại khi quá trình cài đặt hoàn tất.
- Bản di động sẽ thay thế các tệp ứng dụng đã giải nén và mở lại trình phát đã cập nhật.

Không khởi động một tiến trình Luna khác trong khi bản cập nhật đang được áp dụng.

## 21. Trợ giúp và thông tin bản phát hành

- **Trợ giúp > Hướng dẫn sử dụng** hoặc <kbd>F1</kbd> mở hướng dẫn đã cài đặt phù hợp với ngôn ngữ giao diện của Luna. Nếu không có hướng dẫn theo ngôn ngữ hoặc vùng tương ứng, Luna sẽ mở hướng dẫn bằng tiếng Anh.
- **Trợ giúp > Giới thiệu** hiển thị mô tả, phiên bản, thông tin bản quyền và liên kết đến trang web của dự án.
- **Trợ giúp > Ghi chú phát hành** mở trang phát hành GitHub cho phiên bản đã cài đặt trong trình duyệt mặc định của bạn.

Hướng dẫn sử dụng được cài đặt dưới dạng HTML cục bộ và không yêu cầu kết nối internet. Ghi chú phát hành và trang web dự án yêu cầu kết nối internet.

## 22. Cài đặt và dữ liệu người dùng

Luna lưu trữ dữ liệu người dùng trong thư mục `%APPDATA%\Luna Player`:

| Tệp | Nội dung |
| --- | --- |
| `settings.json` | Các tùy chọn và ghi đè phím tắt |
| `bookmarks.json` | Các dấu trang có tên cho tệp cục bộ |
| `positions.json` | Vị trí phát cho từng tệp |
| `equalizer.json` | Các cấu hình do người dùng tạo và các thay đổi đối với cấu hình bộ cân bằng âm thanh có sẵn |
| `favorites.json` | Các liên kết video YouTube và luồng yêu thích |
| `iptv-sources.json` | Các nguồn IPTV đã lưu, với các trường tài khoản được bảo vệ cho người dùng Windows hiện tại |
| `recents.json` | Các tệp, thư mục và danh sách phát đã mở gần đây |
| `Playlists/` | Các danh sách phát JSON có tên, bao gồm mô tả và tham chiếu thư mục |
| `radio.json` | Các đài phát thanh yêu thích và lịch sử nghe radio cục bộ |
| `spotify-preview-searches.txt` | Các truy vấn tìm kiếm Spotify gần đây |
| `spotify-token.dat` | Ủy quyền duyệt Spotify, được bảo vệ cho người dùng Windows hiện tại |
| `spotify-playback-credentials.dat` | Ủy quyền phát nhạc Spotify, được bảo vệ cho người dùng Windows hiện tại |

Sử dụng **Tùy chọn > Sao lưu và khôi phục > Mở thư mục cài đặt người dùng** để mở chính xác thư mục này thay vì phải tự gõ đường dẫn.

Không chỉnh sửa các tệp này trong khi Luna đang chạy. Hãy sử dụng các điều khiển xuất và nhập cho cài đặt và dấu trang bất cứ khi nào có thể.

Nếu không thể đọc `settings.json` hoặc tệp chứa các giá trị không hợp lệ, Luna sẽ khởi động với các giá trị mặc định, hiển thị lý do và bảo vệ tệp hiện có không bị ghi đè. Hãy nhập một bản sao lưu cài đặt hợp lệ hoặc sử dụng **Đặt lại cài đặt** để thay thế có chủ đích. Các tệp dấu trang, mục yêu thích, cấu hình bộ cân bằng, nguồn IPTV và mục gần đây không hợp lệ cũng được báo cáo thay vì bị thay thế âm thầm.

## 23. Bảng tra cứu phím tắt mặc định đầy đủ

Đây là các phím tắt mặc định xuất xưởng. Các gán phím hiện tại của bạn có thể khác sau khi tùy chỉnh. Danh sách chính thức cho bản cài đặt của bạn nằm trong **Tùy chọn > Phím tắt bàn phím** và **Phím tắt toàn cục**.

### Tệp, thông tin và Tùy chọn

| Hành động | Phím tắt chính | Phím tắt phụ |
| --- | --- | --- |
| Mở tệp | <kbd>Ctrl</kbd>+<kbd>O</kbd> | — |
| Mở đĩa DVD | Không có | — |
| Thêm tệp vào danh sách hiện tại | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>F</kbd> | — |
| Thêm thư mục vào danh sách hiện tại | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>D</kbd> | — |
| Mở liên kết luồng mạng | <kbd>Ctrl</kbd>+<kbd>L</kbd> | — |
| Mở tất cả tệp trong thư mục | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd> | — |
| Hiển thị tệp hiện tại trong File Explorer | <kbd>Ctrl</kbd>+<kbd>F</kbd> | — |
| Thuộc tính tệp Windows | <kbd>Alt</kbd>+<kbd>Enter</kbd> | — |
| Hộp thoại các tệp đang mở | <kbd>F2</kbd> | — |
| Đóng tệp hiện tại | <kbd>Ctrl</kbd>+<kbd>W</kbd> | — |
| Đóng tất cả tệp | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>W</kbd> | — |
| Tùy chọn | <kbd>Ctrl</kbd>+<kbd>P</kbd> | — |
| Đọc thông tin tệp hiện tại | <kbd>F</kbd> | — |
| Đọc tiêu đề phương tiện hiện tại | <kbd>I</kbd> | — |
| Thoát Luna | Không có | — |

### Điều hướng danh sách phát và chế độ

| Hành động | Phím tắt chính | Phím tắt phụ |
| --- | --- | --- |
| Tệp trước đó | <kbd>Page Up</kbd> | <kbd>Shift</kbd>+<kbd>Tab</kbd> |
| Tệp tiếp theo | <kbd>Page Down</kbd> | <kbd>Tab</kbd> |
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
| Về đầu | <kbd>Home</kbd> | — |
| Đến cuối | <kbd>End</kbd> | — |
| Đến thời gian | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>G</kbd> | — |
| Chọn sound card | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>A</kbd> | — |
| Bật hoặc tắt toàn màn hình | <kbd>F11</kbd> | — |
| Mở danh sách luồng âm thanh | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>T</kbd> | — |
| Chương trước | <kbd>Ctrl</kbd>+<kbd>Page Up</kbd> | — |
| Chương tiếp theo | <kbd>Ctrl</kbd>+<kbd>Page Down</kbd> | — |
| Đọc tên chương hiện tại | <kbd>C</kbd> | — |
| Chế độ sách nói | Không có | — |
| Dừng khi hết chương hiện tại | Không có | — |
| Luồng âm thanh trước đó | <kbd>Shift</kbd>+<kbd>Page Up</kbd> | — |
| Luồng âm thanh tiếp theo | <kbd>Shift</kbd>+<kbd>Page Down</kbd> | — |
| Bật hoặc tắt đọc phụ đề | <kbd>Ctrl</kbd>+<kbd>U</kbd> | — |
| Nạp phụ đề từ tệp | <kbd>Ctrl</kbd>+<kbd>J</kbd> | — |
| Nạp phụ đề từ địa chỉ web | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>J</kbd> | — |
| Ghi nhớ phụ đề cho phiên này | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>U</kbd> | — |

### Chọn khoảng cách tua

| Hành động | Phím tắt |
| --- | --- |
| Chọn bước tua 1 giây | <kbd>Shift</kbd>+<kbd>1</kbd> |
| Chọn bước tua 5 giây | <kbd>Shift</kbd>+<kbd>2</kbd> |
| Chọn bước tua 10 giây | <kbd>Shift</kbd>+<kbd>3</kbd> |
| Chọn bước tua 20 giây | <kbd>Shift</kbd>+<kbd>4</kbd> |
| Chọn bước tua 30 giây | <kbd>Shift</kbd>+<kbd>5</kbd> |
| Chọn bước tua 1 phút | <kbd>Shift</kbd>+<kbd>6</kbd> |
| Chọn bước tua 2 phút | <kbd>Shift</kbd>+<kbd>7</kbd> |
| Chọn bước tua 3 phút | <kbd>Shift</kbd>+<kbd>8</kbd> |
| Chọn bước tua 5 phút | <kbd>Shift</kbd>+<kbd>9</kbd> |
| Chọn bước tua 10 phút | <kbd>Shift</kbd>+<kbd>0</kbd> |
| Chọn bước tua tùy chỉnh | <kbd>Shift</kbd>+<kbd>-</kbd> |

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
| 100% | <kbd>Ctrl</kbd>+<kbd>0</kbd> | 100% thay thế | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>0</kbd> |

### Âm lượng và trạng thái giọng đọc

| Hành động | Phím tắt |
| --- | --- |
| Tăng âm lượng | Mũi tên <kbd>Lên</kbd> |
| Giảm âm lượng | Mũi tên <kbd>Xuống</kbd> |
| Đặt âm lượng tối đa | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Lên</kbd> |
| Đặt âm lượng tối thiểu | <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Xuống</kbd> |
| Đọc âm lượng | <kbd>V</kbd> |
| Đọc thời gian đã phát | <kbd>E</kbd> |
| Đọc thời gian còn lại | <kbd>R</kbd> |
| Đọc tổng thời lượng | <kbd>T</kbd> |
| Đọc phần trăm vị trí | <kbd>P</kbd> |
| Đọc tốc độ phát | <kbd>S</kbd> |
| Chuyển mức chi tiết giọng đọc Người mới bắt đầu/Nâng cao | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>V</kbd> |

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
| Cân bằng sang trái | <kbd>Ctrl</kbd>+<kbd>Trái</kbd> |
| Cân bằng sang phải | <kbd>Ctrl</kbd>+<kbd>Phải</kbd> |
| Đọc cân bằng âm thanh | <kbd>B</kbd> |
| Bật/tắt âm thanh không gian cho tai nghe | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>H</kbd> |
| Xoay âm thanh không gian sang trái | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Trái</kbd> |
| Xoay âm thanh không gian sang phải | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Phải</kbd> |
| Tăng góc nâng âm thanh không gian | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Lên</kbd> |
| Giảm góc nâng âm thanh không gian | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Xuống</kbd> |
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
| Đánh dấu hoặc bỏ đánh dấu tất cả tệp | <kbd>Ctrl</kbd>+<kbd>A</kbd> |
| Xóa tất cả đánh dấu | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>K</kbd> |
| Đọc số lượng tệp đã đánh dấu | <kbd>K</kbd> |
| Sao chép các tệp đã đánh dấu vào thư mục | Không có |
| Di chuyển các tệp đã đánh dấu vào thư mục | Không có |
| Sao chép các tệp đã đánh dấu vào bảng nhớ tạm | Không có |
| Xóa các tệp đã đánh dấu | Không có |

### Dấu trang

| Hành động | Phím tắt |
| --- | --- |
| Thêm dấu trang | <kbd>Shift</kbd>+<kbd>M</kbd> |
| Quản lý dấu trang | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>M</kbd> |
| Nhảy đến vị trí dấu trang từ 1–9 | <kbd>Alt</kbd>+<kbd>1</kbd> đến <kbd>Alt</kbd>+<kbd>9</kbd> |
| Nhảy đến vị trí dấu trang 10 | <kbd>Alt</kbd>+<kbd>0</kbd> |

### Dịch vụ trực tuyến, công cụ, ghi âm, cập nhật và trợ giúp

| Hành động | Phím tắt |
| --- | --- |
| Mở liên kết YouTube | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Y</kbd> |
| Tìm kiếm trên YouTube | <kbd>Ctrl</kbd>+<kbd>Y</kbd> |
| Video yêu thích | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd> |
| Mở hàng đợi tải xuống | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> |
| Thêm video YouTube hiện tại vào hàng đợi | <kbd>Ctrl</kbd>+<kbd>Q</kbd> |
| Tải xuống trực tiếp video YouTube hiện tại | <kbd>Ctrl</kbd>+<kbd>D</kbd> |
| Chọn tùy chọn tải xuống video YouTube hiện tại | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>D</kbd> |
| Xem mô tả video hiện tại | <kbd>Alt</kbd>+<kbd>D</kbd> |
| Sao chép liên kết video hiện tại | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>C</kbd> |
| Mở video hiện tại trong trình duyệt | Không có |
| Mở kênh của video hiện tại trong trình duyệt | Không có |
| Duyệt kênh của video hiện tại trong Luna | Không có |
| Mở trình duyệt radio | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>R</kbd> |
| Mở trình duyệt Spotify | <kbd>Alt</kbd>+<kbd>S</kbd> |
| Tùy chọn Spotify | Không có |
| Mở bộ chuyển đổi phương tiện | Không có |
| Quản lý nguồn IPTV | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>I</kbd> |
| Duyệt kênh IPTV | Không có |
| Đặt hẹn giờ ngủ | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>S</kbd> |
| Hủy hẹn giờ ngủ | Không có |
| Đọc thời gian hẹn giờ ngủ còn lại | <kbd>Ctrl</kbd>+<kbd>T</kbd> |
| Mở giao diện ghi âm | <kbd>Alt</kbd>+<kbd>R</kbd> |
| Bắt đầu ghi âm | <kbd>F9</kbd> |
| Tạm dừng hoặc tiếp tục ghi âm | <kbd>F7</kbd> |
| Dừng ghi âm | <kbd>F8</kbd> |
| Mở thư mục ghi âm | Không có |
| Mở hướng dẫn sử dụng | <kbd>F1</kbd> |
| Giới thiệu về Luna | Không có |
| Mở ghi chú phát hành | Không có |
| Kiểm tra cập nhật ứng dụng | Không có |
| Cập nhật các thành phần YouTube | Không có |

### Phím tắt toàn cục mặc định

Các lệnh này hoạt động trong khi một ứng dụng khác đang giữ tiêu điểm, bao gồm cả khi Luna đang được thu nhỏ. Chúng bị bỏ qua trong khi Luna hoặc một trong các hộp thoại của nó đang giữ tiêu điểm.

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

## 24. Các định dạng được hỗ trợ

Luna lọc các lựa chọn tệp và thư mục bằng các phần mở rộng sau. Việc phát lại được xử lý bởi mpv, vì vậy các định dạng khác được công cụ đó hỗ trợ cũng có thể phát được khi mở trực tiếp.

**Âm thanh:** AAC, AC-3, AIFF, ALAC, APE, AU, DTS, E-AC-3, FLAC, M4A, M4B, MKA, MP1, MP2, MP3, MPC, OGA, OGG, OGM, Opus, TAK, TrueHD (`.thd`), TTA, WAV, WMA và WavPack.

**Video:** 3G2, 3GP, AVI, FLV, IVF, M2TS, M4V, MJ2, MKV, MOV, MP4, MPEG, MPG, MXF, OGV, RMVB, TS, WebM, WMV và Y4M.

**Danh sách phát:** M3U và M3U8, bao gồm các luồng HLS, cùng danh sách phát JSON cục bộ của Luna với mô tả và tham chiếu thư mục.

**Đĩa:** DVD-Video và DVD-Audio thông qua **Tệp > Mở DVD...**, tính năng AutoPlay của DVD hoặc thao tác phát DVD của File Explorer. Menu đĩa, đĩa Blu-ray và Audio CD không được hỗ trợ bởi tính năng mở đĩa này.

## 25. Khắc phục sự cố

### Một tệp hoặc thư mục mở ít mục hơn dự kiến

Kiểm tra **Tùy chọn > Chung > Bạn muốn mở gì cùng với các tệp?**. Mặc định chỉ mở tệp đã chọn. Các lệnh mở thư mục chỉ bao gồm các phần mở rộng được nhận dạng là phương tiện. Các thư mục không thể truy cập và thư mục hệ thống sẽ bị bỏ qua trong quá trình quét đệ quy.

### Một luồng phát trực tiếp không có thời lượng hoặc phần trăm

Các luồng phát trực tiếp thường không công bố độ dài cố định. Chuyển đến thời gian, nhảy theo phần trăm, thời gian còn lại và tua đến cuối yêu cầu phải biết trước thời lượng tổng thể nên có thể không khả dụng.

### Video YouTube không mở được

Hãy thử các bước sau theo thứ tự:

1. Xác nhận rằng địa chỉ mở được trong trình duyệt và trỏ đến một video hoặc danh sách phát chứ không phải một kênh.
2. Chọn **Trợ giúp > Cập nhật > Cập nhật các thành phần YouTube**.
3. Nếu các thành phần chưa được cài đặt, chọn **Tùy chọn > YouTube > Tải xuống các thành phần YouTube**.
4. Hãy thử kênh cập nhật Ổn định (Stable) trước; chỉ sử dụng Nightly hoặc Master nếu kênh Stable không thể xử lý thay đổi dịch vụ gần đây của YouTube.
5. Đối với video bị giới hạn độ tuổi, chỉ dành cho hội viên hoặc được bảo vệ bằng xác minh, hãy định cấu hình tệp cookie hoặc cookie Firefox trong Tùy chọn YouTube.

### Tải xuống YouTube đã tải xong dữ liệu nhưng vẫn tiếp tục hoạt động

Sau khi yt-dlp nhận đủ toàn bộ dữ liệu phương tiện, ứng dụng vẫn có thể cần ghép luồng video và âm thanh, chuyển đổi kết quả, nhúng thông tin, xử lý các chương hoặc di chuyển tệp đã hoàn thành. Tiêu đề và văn bản của cửa sổ tiến trình sẽ nêu tên công việc này. Đừng hủy chỉ vì phần trăm đã đạt 100%.

### Quá trình tải xuống tiếp tục sau khi cửa sổ tiến trình đóng lại

Việc chọn **Thu nhỏ về hàng đợi** sẽ cố ý chuyển tiến trình hiển thị sang **Công cụ > Hàng đợi tải xuống...**. Đóng cửa sổ hàng đợi cũng vẫn để công việc đang hoạt động tiếp tục chạy. Hãy dừng mục đó từ hàng đợi nếu bạn không còn muốn tải nữa, hoặc đóng Luna và xác nhận rằng các tệp tải chưa hoàn tất sẽ bị hủy.

### IPTV không có thông tin chương trình

Thông tin chương trình chỉ xuất hiện khi nhà cung cấp cung cấp thông tin đó hoặc nguồn có địa chỉ EPG XMLTV khớp. Mã định danh kênh trong hướng dẫn phải khớp với mã định danh của nguồn. Quá trình phát vẫn có thể hoạt động bình thường khi không có hướng dẫn chương trình.

### Spotify không đăng nhập được hoặc không nạp được bộ sưu tập

Kiểm tra Client ID trong **Tùy chọn > Spotify** và URI chuyển hướng trong ứng dụng nhà phát triển của bạn. URI phải chính xác là `http://127.0.0.1:43891/callback/`, bao gồm cả dấu gạch chéo cuối cùng. Hãy thực hiện cả hai lần ủy quyền trình duyệt nếu Luna yêu cầu: duyệt và phát âm thanh đăng nhập riêng biệt. Nếu ủy quyền đã hết hạn, hãy đăng nhập lại.

Nếu Spotify từ chối một danh sách phát hoặc bộ sưu tập, hãy kiểm tra xem tài khoản của bạn có thể truy cập nội dung đó hay không và có được phép sử dụng ứng dụng nhà phát triển của bạn hay không. Ứng dụng ở chế độ phát triển không thể truy cập mọi loại nội dung của Spotify. Nếu Luna báo cáo giới hạn yêu cầu, hãy đợi trong khoảng thời gian được chỉ định trước khi thử lại; việc làm mới liên tục sẽ không xóa được giới hạn. Một bộ sưu tập không thể nạp do Spotify hạn chế sẽ không thể sử dụng được bằng cách thay đổi chất lượng âm thanh của Luna.

### Đài phát thanh không phát hoặc không có thông tin bài hát

Hãy thử **Làm mới** trong trình duyệt radio và chọn một đài khác. Lần kiểm tra thành công gần nhất của thư mục công khai không đảm bảo rằng luồng phát hiện đang khả dụng hoặc có thể nghe được tại khu vực của bạn. Nếu không có đài nào xuất hiện, hãy xóa tìm kiếm và kiểm tra cả bộ lọc danh mục và bộ lọc toàn cục. **Chỉ các đài đang hoạt động** và **Chỉ HTTPS** có thể loại trừ các đài mà bạn mong muốn tìm thấy.

Không phải đài phát thanh nào cũng gửi tiêu đề bài hát hoặc nghệ sĩ. Thông tin phát sóng bị thiếu hoặc không thay đổi không nhất thiết có nghĩa là quá trình phát đã dừng lại. Sử dụng **Chi tiết đài phát thanh** để kiểm tra mục thư mục, hoặc **Mở trang web** từ menu ngữ cảnh của đài để kiểm tra trang web riêng của nhà đài.

### Âm thanh không gian cho tai nghe không thể mở bộ dữ liệu

Tải xuống bộ dữ liệu `.sofa` từ các liên kết trong mục [Âm thanh không gian cho tai nghe](#âm-thanh-không-gian-cho-tai-nghe-headphone-spatialization), sau đó chọn chính tệp đó trong Tùy chọn. Một trang HTML đã lưu, tệp nén lưu trữ hoặc tệp âm thanh thông thường không phải là bộ dữ liệu HRTF. Hãy giữ tệp đã chọn ở vị trí cố định; các bản sao lưu cài đặt không chứa tệp này. Nếu một bộ dữ liệu hợp lệ không thể mở được từ đường dẫn chứa các ký tự không phải ASCII, hãy thử chuyển sang thư mục và tên tệp chỉ sử dụng các chữ cái tiếng Anh thông thường và chọn lại.

Để so sánh các hướng âm thanh, hãy nghe qua tai nghe, tắt **Phát âm thanh dạng Mono**, và bắt đầu ở góc nâng gần 0 độ. Giảm độ khuếch đại nếu âm thanh bị rè hoặc méo tiếng. Nếu cảm giác vị trí trước/sau hoặc trên/dưới chưa thuyết phục, hãy thử một bộ dữ liệu khác thay vì cho rằng tệp có dung lượng lớn hơn sẽ nghe hay hơn đối với bạn.

### Tính năng ghi âm không tìm thấy chương trình

Ghi âm chương trình yêu cầu Windows 10 phiên bản 2004 trở lên. Chương trình đó cũng phải có phiên âm thanh Windows đang hoạt động. Hãy bắt đầu phát âm thanh trong chương trình đó, mở lại hộp thoại nguồn và chọn chương trình từ danh sách đã được làm mới.

### Ghi âm bắt đầu nhưng thiếu một trong các nguồn

Luna có thể tiếp tục khi có ít nhất một nguồn mở thành công. Hãy đọc cảnh báo liệt kê các nguồn bị lỗi, sau đó kiểm tra xem thiết bị của chúng đã được kết nối, kích hoạt và khả dụng hay chưa. Dừng ghi âm trước khi chỉnh sửa danh sách nguồn.

### Phím tắt toàn cục không hoạt động

Phím tắt toàn cục chỉ hoạt động trong khi một ứng dụng khác đang giữ tiêu điểm. Chuyển sang ứng dụng khác bằng Alt+Tab hoặc thu nhỏ Luna, sau đó sử dụng phím tắt. Trong khi Luna hoặc một trong các hộp thoại của nó đang giữ tiêu điểm, hãy sử dụng các phím tắt cục bộ thay thế. Kiểm tra gán phím trong **Tùy chọn > Phím tắt toàn cục**. Nếu tổ hợp phím đó cũng thực hiện một hành động của Windows, hãy gán các phím khác; Luna không chặn việc xử lý các tổ hợp phím này của hệ điều hành.

### Cài đặt không lưu sau khi gặp lỗi khởi động

Luna bảo vệ tệp `settings.json` không hợp lệ hoặc không thể đọc được thay vì ghi đè lên nó. Mở Tùy chọn và nhập bản sao lưu cài đặt hợp lệ hoặc chọn **Sao lưu và khôi phục > Đặt lại cài đặt**. Nếu bạn cần tệp gốc để chẩn đoán, hãy sao chép tệp đó từ thư mục cài đặt người dùng trước.

### Dấu trang hoặc mục yêu thích báo cáo dữ liệu không hợp lệ

Tệp lưu trữ không hợp lệ được bảo toàn thay vì bị thay thế âm thầm. Mở thư mục cài đặt người dùng, tạo bản sao của tệp JSON bị ảnh hưởng và khôi phục bản sao lưu hoạt động tốt đã biết. Dấu trang có thể được khôi phục thông qua Tùy chọn. Mục yêu thích hiện yêu cầu khôi phục chính tệp `favorites.json` trong khi Luna đang đóng.

### Hướng dẫn sử dụng không mở được

Hướng dẫn đã cài đặt phải nằm tại `docs\<mã-ngôn-ngữ>\user-guide.html` bên cạnh tệp thực thi. Hãy giải nén lại toàn bộ tệp nén di động hoặc sửa chữa/cài đặt lại ứng dụng nếu thư mục `docs` bị thiếu. Luna sẽ tự động chuyển từ ngôn ngữ theo vùng sang ngôn ngữ cơ sở và sau đó sang tiếng Anh nếu không tìm thấy bản dịch tương ứng.

## 26. Nhà phát hành và giấy phép

Luna Player được phát hành bởi **Diamond Star**.

Bản quyền © 2026 Diamond Star.

Luna Player là phần mềm tự do được cấp phép theo Giấy phép Công cộng GNU (GPL), phiên bản 3 hoặc (tùy bạn chọn) bất kỳ phiên bản nào sau này (GPL-3.0-or-later). Phần liên kết mpv được dịch trong `src/Mpv.cs` được cấp phép theo Giấy phép Công cộng GNU Rút gọn (LGPL), phiên bản 2.1 hoặc mới hơn.

Luna Player cũng sử dụng các thành phần bên thứ ba được cấp phép riêng biệt, bao gồm mpv, FFmpeg, wxWidgets, Prism, NAudio và PyYt. Mỗi thành phần vẫn tuân theo giấy phép riêng của nó. Xem tệp `NOTICE.txt`, được cài đặt cùng với Luna Player, để biết các tuyên bố bản quyền, phiên bản thành phần, tên giấy phép và vị trí nguồn. Các văn bản giấy phép liên quan được cài đặt trong thư mục `licenses`.

Tóm tắt giấy phép trong hướng dẫn này được cung cấp để thuận tiện tham khảo. Tệp `LICENSE.txt`, `NOTICE.txt` và các tệp trong thư mục `licenses` chứa các điều khoản và ghi công chính thức. Trong kho lưu trữ mã nguồn, các tệp cấp cao nhất tương ứng có tên là `LICENSE` và `NOTICE`.

## 27. Liên hệ và hỗ trợ

Luna Player được phát triển và phát hành bởi Diamond Star. Bạn có thể sử dụng các kênh sau:

- **Email:** [ramymaherali55@gmail.com](mailto:ramymaherali55@gmail.com)
- **Telegram:** [Liên hệ Diamond Star trên Telegram](https://t.me/diamondStar35)
- **Báo cáo lỗi:** [Các vấn đề của Luna Player trên GitHub](https://github.com/diamondStar35/luna_player/issues)
- **Mã nguồn và bản phát hành:** [Luna Player trên GitHub](https://github.com/diamondStar35/luna_player)

Đối với báo cáo lỗi, vui lòng gửi kèm phiên bản Luna được hiển thị trong **Trợ giúp > Giới thiệu**, phiên bản Windows của bạn, điều bạn mong đợi, điều thực tế đã xảy ra và các bước ngắn nhất để tái hiện sự cố. Đính kèm nguyên văn bất kỳ thông báo lỗi nào. Hãy xóa tên tệp riêng tư, địa chỉ web, chi tiết tài khoản và thông tin cá nhân khác khỏi nhật ký hoặc ảnh chụp màn hình trước khi chia sẻ chúng công khai.
