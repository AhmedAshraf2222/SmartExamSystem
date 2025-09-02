import cv2
import numpy as np
import os
import argparse

def create_blank_sheet(paper_size=(2480, 2806)):
    """إنشاء ورقة بيضاء فارغة."""
    return np.ones((paper_size[1], paper_size[0], 3), dtype=np.uint8) * 255

def draw_header(image, title, course_name, course_code, course_level, term, num_questions_val, exam_date, full_mark, exam_time, department, college_name, university_name, model_no=None, paper_size=(2480, 2806)):
    """رسم قسم رأس الصفحة مقسمًا إلى عمودين."""
    center_x = paper_size[0] // 2
    y_content_start = 50  # تقليل المسافة العلوية
    
    # تقسيم الورقة إلى عمودين
    col_margin_left = 80  # تقليل الهوامش لتجنب التداخل مع الإطار
    col_margin_right = paper_size[0] - 80
    divider_x = paper_size[0] // 2 
    
    current_y_left_col_content_start = y_content_start 
    col_x_left_content_start = col_margin_left 
    row_height_right = 70  # تقليل ارتفاع الصف
    label_col_width_right = 300 

    properties = [
        ("University Name:", university_name),
        ("College Name:", college_name),
        ("Course Name:", course_name),
        ("Course Code:", course_code),
        ("Course Level:", course_level),
        ("Term:", term),
        ("Department:", department),
        ("Date:", exam_date),
        ("Full Mark:", full_mark),
        ("Time:", exam_time),
        ("No. of Questions:", num_questions_val),
    ]
    
    for i, (label, value) in enumerate(properties):
        current_row_y_left = current_y_left_col_content_start + i * row_height_right 
        cv2.putText(image, label, (col_x_left_content_start, current_row_y_left), 
                    cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 0, 0), 2, cv2.LINE_AA) 
        cv2.putText(image, str(value), (col_x_left_content_start + label_col_width_right + 20, current_row_y_left + 5), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 0, 0), 2, cv2.LINE_AA) 
    
    final_y_left_col_content = current_y_left_col_content_start + len(properties) * row_height_right

    current_y_right_col_content_start = y_content_start 
    col_x_right_content_start = divider_x + 40 

    name_text = "Name:"
    cv2.putText(image, name_text, (col_x_right_content_start, current_y_right_col_content_start), 
                cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 0, 0), 2, cv2.LINE_AA)
    
    current_y_after_name = current_y_right_col_content_start + 70 
    seat_num_label = "Seat Num:"
    text_size_seat = cv2.getTextSize(seat_num_label, cv2.FONT_HERSHEY_SIMPLEX, 1.0, 2)[0] 
    cv2.putText(image, seat_num_label, (col_x_right_content_start, current_y_after_name), 
                cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 0, 0), 2, cv2.LINE_AA)
        
    radius_bubble = 25 
    gap_x_bubble = 60 
    seat_bubbles_common_start_x_new_right_col = col_x_right_content_start + 230 
    num_id_digits = 4 
    digit_labels = ["Units:", "Tens:", "Hundreds:", "Thousands:"] 
    start_y_actual_seat_bubbles = current_y_after_name + text_size_seat[1] + 50 
    label_x_offset_new_right_col = col_x_right_content_start + 20 

    for digit_pos in range(num_id_digits): 
        row_y_bubble = start_y_actual_seat_bubbles + digit_pos * (radius_bubble * 2 + 20) 
        label_text = digit_labels[digit_pos]
        cv2.putText(image, label_text, (label_x_offset_new_right_col, row_y_bubble + cv2.getTextSize(label_text, cv2.FONT_HERSHEY_SIMPLEX, 0.9, 2)[0][1] // 2), 
                    cv2.FONT_HERSHEY_SIMPLEX, 0.9, (0, 0, 0), 2, cv2.LINE_AA)

        for i in range(10): 
            center_x_bubble = seat_bubbles_common_start_x_new_right_col + i * gap_x_bubble 
            center_y_bubble = row_y_bubble
            number_text = str(i) 
            text_size_num_in_bubble = cv2.getTextSize(number_text, cv2.FONT_HERSHEY_SIMPLEX, 0.8, 2)[0]
            text_x_num_in_bubble = center_x_bubble - text_size_num_in_bubble[0] // 2
            text_y_num_in_bubble = center_y_bubble + text_size_num_in_bubble[1] // 2
            cv2.circle(image, (center_x_bubble, center_y_bubble), radius_bubble, (0, 0, 0), 2, cv2.LINE_AA)
            cv2.putText(image, number_text, (text_x_num_in_bubble, text_y_num_in_bubble),
                        cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 0, 0), 2, cv2.LINE_AA)

    final_y_seat_num_section = start_y_actual_seat_bubbles + num_id_digits * (radius_bubble * 2 + 20) 

    if model_no:
        model_no_label = "Model No.:"
        current_y_model_no_section = final_y_seat_num_section + 40 
        cv2.putText(image, model_no_label, (col_x_right_content_start, current_y_model_no_section), 
                    cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 0, 0), 2, cv2.LINE_AA) 
        
        num_choices_model_no = 6 
        model_bubble_radius = 25 
        model_bubble_gap_x = 60 
        model_bubbles_start_x = seat_bubbles_common_start_x_new_right_col 
        start_y_model_bubbles_base = current_y_model_no_section + 30 
        
        shade_model_index = ord(model_no.upper()) - ord('A') if model_no and isinstance(model_no, str) and model_no.isalpha() and len(model_no) == 1 and 'A' <= model_no.upper() <= chr(ord('A') + num_choices_model_no - 1) else -1

        for i in range(num_choices_model_no):
            char_to_display = chr(65 + i) 
            center_x_model_bubble = model_bubbles_start_x + i * model_bubble_gap_x
            center_y_model_bubble = start_y_model_bubbles_base
            if i == shade_model_index:
                cv2.circle(image, (center_x_model_bubble, center_y_model_bubble), model_bubble_radius, (0, 0, 0), cv2.FILLED, cv2.LINE_AA) 
            else:
                cv2.circle(image, (center_x_model_bubble, center_y_model_bubble), model_bubble_radius, (0, 0, 0), 2, cv2.LINE_AA) 
                cv2.putText(image, char_to_display, (center_x_model_bubble - cv2.getTextSize(char_to_display, cv2.FONT_HERSHEY_SIMPLEX, 0.8, 2)[0][0] // 2, center_y_model_bubble + cv2.getTextSize(char_to_display, cv2.FONT_HERSHEY_SIMPLEX, 0.8, 2)[0][1] // 2),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 0, 0), 2, cv2.LINE_AA) 
        
        final_y_model_no_section_content = start_y_model_bubbles_base + (model_bubble_radius * 2) + 15 
    else:
        final_y_model_no_section_content = final_y_seat_num_section 

    final_y_after_header_section = max(final_y_model_no_section_content, final_y_left_col_content) + 40 
    cv2.line(image, (80, final_y_after_header_section), (paper_size[0] - 80, final_y_after_header_section), (0, 0, 0), 2)
    cv2.line(image, (divider_x, y_content_start - 25), (divider_x, final_y_after_header_section), (0, 0, 0), 2)
    return final_y_after_header_section

def add_answer_bubbles(image, num_questions, choices=4, student_info_y=400, paper_size=(2480, 2806)):
    """إضافة فقاعات الإجابة."""
    start_y = student_info_y + 80
    bubble_radius = 28 
    question_to_bubble_gap = 50 
    bubble_inner_spacing = 140 
    num_columns = 2
    row_spacing = 85
    col_margin = 120 
    
    if num_questions > 48: 
        num_columns = 3
        row_spacing = 65
        bubble_radius = 23
        col_margin = 50
        bubble_inner_spacing = 90
        question_to_bubble_gap = 45

    col_width = (paper_size[0] - 2 * col_margin) / num_columns 
    column_starts_x = [col_margin + i * col_width for i in range(num_columns)]
    num_label_width = cv2.getTextSize("00.", cv2.FONT_HERSHEY_SIMPLEX, 1.0, 2)[0][0]
    question_offset_from_col_start = 0 
    bubble_offset_from_question = num_label_width + question_to_bubble_gap 

    column_question_counts = []
    if num_columns == 2:
        questions_in_col1 = min(24, num_questions)
        questions_in_col2 = num_questions - questions_in_col1
        column_question_counts = [questions_in_col1, questions_in_col2]

    elif num_columns == 3:
        questions_in_col1 = min(25, num_questions) 
        remaining_questions_after_col1 = num_questions - questions_in_col1
        questions_in_col2 = min(25, remaining_questions_after_col1) 
        questions_in_col3 = remaining_questions_after_col1 - questions_in_col2 
        column_question_counts = [questions_in_col1, questions_in_col2, questions_in_col3]
    
    current_question_offset = 0 

    for col_idx in range(num_columns):
        col_start_x = column_starts_x[col_idx]
        col_questions_count = column_question_counts[col_idx]

        for i in range(col_questions_count):
            question_index_in_col = i 
            question_number = current_question_offset + 1 
            question_y = start_y + question_index_in_col * row_spacing
            question_num_x = col_start_x + question_offset_from_col_start
            bubbles_start_current_x = col_start_x + bubble_offset_from_question

            cv2.putText(image, f"{question_number:02d}.", (int(question_num_x), int(question_y)),
                        cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 0, 0), 2, cv2.LINE_AA)

            for choice in range(choices):
                center_x = int(bubbles_start_current_x + bubble_inner_spacing * choice)
                center_y = int(question_y - 5) 
                letter = chr(65 + choice)
                text_font_scale = 1.0 if num_columns == 2 else 0.8
                cv2.circle(image, (center_x, center_y), bubble_radius, (0, 0, 0), 2, cv2.LINE_AA)
                text_size = cv2.getTextSize(letter, cv2.FONT_HERSHEY_SIMPLEX, text_font_scale, 2)[0]
                text_x = center_x - text_size[0] // 2
                text_y = center_y + text_size[1] // 2
                cv2.putText(image, letter, (text_x, text_y),
                            cv2.FONT_HERSHEY_SIMPLEX, text_font_scale, (0, 0, 0), 2, cv2.LINE_AA)
            
            current_question_offset += 1 

    max_questions_in_any_column = max(column_question_counts) if column_question_counts else 0
    return start_y + (max_questions_in_any_column * row_spacing) + 120

def generate_bubble_sheet(title, course_name, course_code, course_level, term, num_questions_val, exam_date, full_mark, exam_time, department, college_name, university_name, model_no, output_dir):
    """إنشاء ورقة بابل شيت كاملة."""
    os.makedirs(output_dir, exist_ok=True)
    paper_size = (2336, 3308)
    sheet = create_blank_sheet(paper_size)
    info_y = draw_header(sheet, title, course_name, course_code, course_level, term, num_questions_val, exam_date, full_mark, exam_time, department, college_name, university_name, model_no, paper_size)
    add_answer_bubbles(sheet, num_questions_val, choices=4, student_info_y=info_y, paper_size=paper_size)
    output_path = os.path.join(output_dir, f"bubble_sheet_model_{model_no}_{num_questions_val}_Q.png")
    cv2.imwrite(output_path, sheet)
    return output_path

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate bubble sheets for exams.")
    parser.add_argument("--title", required=True, help="Exam title")
    parser.add_argument("--course_name", required=True, help="Course name")
    parser.add_argument("--course_code", required=True, help="Course code")
    parser.add_argument("--course_level", required=True, help="Course level")
    parser.add_argument("--term", required=True, help="Term (e.g., First Term)")
    parser.add_argument("--num_questions", type=int, required=True, help="Number of questions")
    parser.add_argument("--exam_date", required=True, help="Exam date (e.g., DD/MM/YYYY)")
    parser.add_argument("--full_mark", required=True, help="Full mark")
    parser.add_argument("--exam_time", required=True, help="Exam duration (e.g., 3 Hours)")
    parser.add_argument("--department", required=True, help="Department")
    parser.add_argument("--college_name", required=True, help="College name")
    parser.add_argument("--university_name", required=True, help="University name")
    parser.add_argument("--models", required=True, help="Comma-separated list of model IDs (e.g., A,B,C)")
    parser.add_argument("--output_dir", required=True, help="Output directory for bubble sheets")
    args = parser.parse_args()

    models_to_generate = args.models.split(',')
    for model in models_to_generate:
        output_path = generate_bubble_sheet(
            title=args.title,
            course_name=args.course_name,
            course_code=args.course_code,
            course_level=args.course_level,
            term=args.term,
            num_questions_val=args.num_questions,
            exam_date=args.exam_date,
            full_mark=args.full_mark,
            exam_time=args.exam_time,
            department=args.department,
            college_name=args.college_name,
            university_name=args.university_name,
            model_no=model.strip(),
            output_dir=args.output_dir
        )