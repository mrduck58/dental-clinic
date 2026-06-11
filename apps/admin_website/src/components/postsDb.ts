export interface Post {
  id: string;
  title: string;
  category: string;
  author: string;
  date: string;
  status: "Đã xuất bản" | "Bản nháp";
  content: string;
  thumbnail: string;
}

const DEFAULT_POSTS: Post[] = [
  {
    id: "1",
    title: "Chăm sóc răng miệng sau nhổ",
    category: "Chăm sóc răng miệng",
    author: "BS. Nguyễn Minh Đức",
    date: "10/07/2024",
    status: "Đã xuất bản",
    content: "Sau khi nhổ răng, việc chăm sóc răng miệng đúng cách đóng vai trò cực kỳ quan trọng giúp vết thương mau lành và hạn chế tối đa các biến chứng nguy hiểm như nhiễm trùng hay viêm ổ xương răng khô. Trong vòng 24 giờ đầu tiên, bệnh nhân tuyệt đối không được súc miệng mạnh, khạc nhổ hoặc dùng ống hút để tránh làm vỡ cục máu đông - yếu tố cốt lõi giúp cầm máu và bảo vệ ổ xương ổ răng.",
    thumbnail: "https://images.unsplash.com/photo-1579684385127-1ef15d508118?auto=format&fit=crop&w=800&q=80"
  },
  {
    id: "2",
    title: "Kiến thức về Niềng răng",
    category: "Niềng răng",
    author: "BS. Lê Thị Phương Thảo",
    date: "08/07/2024",
    status: "Bản nháp",
    content: "Niềng răng (chỉnh nha) là giải pháp tối ưu giúp khắc phục các khuyết điểm về răng như hô, móm, thưa, lệch lạc, mang lại khớp cắn chuẩn xác và nụ cười tự tin. Hiện nay, có nhiều phương pháp niềng răng phổ biến như niềng răng mắc cài kim loại, mắc cài sứ và niềng răng trong suốt Invisalign đáp ứng nhu cầu thẩm mỹ khác nhau của khách hàng.",
    thumbnail: "https://images.unsplash.com/photo-1606811971618-4486d14f3f99?auto=format&fit=crop&w=800&q=80"
  },
  {
    id: "3",
    title: "Các loại răng sứ phổ biến",
    category: "Phục hình",
    author: "ThS. BS. Nguyễn Minh Đức",
    date: "05/07/2024",
    status: "Đã xuất bản",
    content: "Răng sứ phục hình ngày càng được ưa chuộng nhờ khả năng phục hồi chức năng ăn nhai và cải thiện thẩm mỹ tối đa. Các loại răng sứ phổ biến hiện nay bao gồm răng sứ kim loại (như Titan) giá thành hợp lý nhưng dễ bị đen viền nướu sau một thời gian sử dụng, và răng sứ toàn phần (như Zirconia, Cercon, Emax) có độ bền sinh học cao, màu sắc tự nhiên như răng thật và không bị đổi màu.",
    thumbnail: "https://images.unsplash.com/photo-1588776814546-1ffcf47267a5?auto=format&fit=crop&w=800&q=80"
  },
  {
    id: "4",
    title: "Chương trình khuyến mãi tháng 7",
    category: "Khuyến mãi",
    author: "ThS. BS. Nguyễn Minh Đức",
    date: "01/07/2024",
    status: "Đã xuất bản",
    content: "Chào đón mùa hè rực rỡ, Sơn Giang Dental Clinic mang đến chương trình ưu đãi đặc biệt trong tháng 7 này: Giảm ngay 15% cho các dịch vụ tẩy trắng răng, bọc răng sứ thẩm mỹ và giảm đến 10 triệu đồng cho các gói chỉnh nha Invisalign. Đăng ký ngay hôm nay để nhận được sự tư vấn trực tiếp từ đội ngũ Thạc sĩ, Bác sĩ chuyên khoa hàng đầu.",
    thumbnail: "https://images.unsplash.com/photo-1472289065668-ce650ac443d2?auto=format&fit=crop&w=800&q=80"
  }
];

export function getPosts(): Post[] {
  if (typeof window === "undefined") return DEFAULT_POSTS;
  const postsStr = localStorage.getItem("sg_dental_posts");
  if (!postsStr) {
    localStorage.setItem("sg_dental_posts", JSON.stringify(DEFAULT_POSTS));
    return DEFAULT_POSTS;
  }
  try {
    return JSON.parse(postsStr);
  } catch (e) {
    return DEFAULT_POSTS;
  }
}

export function savePosts(posts: Post[]) {
  if (typeof window === "undefined") return;
  localStorage.setItem("sg_dental_posts", JSON.stringify(posts));
}

export function getPostById(id: string): Post | undefined {
  const posts = getPosts();
  return posts.find(p => p.id === id);
}

export function addPost(post: Omit<Post, "id" | "date">): Post {
  const posts = getPosts();
  const dateObj = new Date();
  const formattedDate = `${String(dateObj.getDate()).padStart(2, "0")}/${String(dateObj.getMonth() + 1).padStart(2, "0")}/${dateObj.getFullYear()}`;
  
  const newPost: Post = {
    ...post,
    id: String(Date.now()),
    date: formattedDate
  };
  
  posts.unshift(newPost);
  savePosts(posts);
  return newPost;
}

export function updatePost(id: string, updatedFields: Partial<Omit<Post, "id" | "date">>): Post | undefined {
  const posts = getPosts();
  const idx = posts.findIndex(p => p.id === id);
  if (idx === -1) return undefined;
  
  const updatedPost = {
    ...posts[idx],
    ...updatedFields
  };
  
  posts[idx] = updatedPost;
  savePosts(posts);
  return updatedPost;
}

export function deletePost(id: string): boolean {
  const posts = getPosts();
  const filtered = posts.filter(p => p.id !== id);
  if (filtered.length === posts.length) return false;
  savePosts(filtered);
  return true;
}
