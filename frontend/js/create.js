function populateExams() {
  const examList = document.getElementById("exam-list");
  examList.innerHTML =
    '<tr><td colspan="12" class="loading-item"><i class="loading-spinner"></i> Loading...</td></tr>';

  fetch("https://localhost:7181/api/Exams", {
    headers: {},
  })
    .then((response) => {
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return response.json();
    })
    .then((exams) => {
      examList.innerHTML = "";
      if (exams && exams.length > 0) {
        exams.forEach((exam) => {
          const row = document.createElement("tr");
          row.innerHTML = `
                        <td>${exam.examId}</td>
                        <td>${exam.examName}</td>
                        <td>${exam.materialName || "N/A"}</td>
                        <td>${exam.mainDegree}</td>
                        <td>${exam.totalProblems}</td>
                        <td>${exam.shuffle ? "Yes" : "No"}</td>
                        <td>${exam.examDuration / 60}</td>
                        <td>${new Date(exam.examDate).toLocaleDateString(
                          "en-GB"
                        )}</td>
                        <td>${exam.universityName || "N/A"}</td>
                        <td>${exam.collegeName || "N/A"}</td>
                        <td><input type="number" min="1" value="1" id="copies-${
                          exam.examId
                        }"></td>
                        <td><button onclick="downloadExam(${
                          exam.examId
                        })">Download</button></td>
                    `;
          examList.appendChild(row);
        });
      } else {
        examList.innerHTML =
          '<tr><td colspan="12" class="no-materials">No exams available</td></tr>';
      }
    })
    .catch((err) => {
      console.error("Error fetching exams:", err);
      examList.innerHTML = `<tr><td colspan="12" class="error-item">Error loading exams: ${err.message}</td></tr>`;
    });
}

function downloadExam(examId) {
  const copiesInput = document.getElementById(`copies-${examId}`);
  const copies = parseInt(copiesInput.value);
  const button = event.target;
  const originalText = button.textContent;

  // Validate copies
  if (isNaN(copies) || copies < 1) {
    alert("Please enter a valid number of copies (at least 1).");
    return;
  }

  button.innerHTML = '<i class="loading-spinner"></i> Downloading...';
  button.disabled = true;

  console.log("Sending request with:", { id: examId, numberOfModels: copies });

  fetch("https://localhost:7181/api/ExamFilesGenerator/GenerateExamFiles", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      id: examId,
      numberOfModels: copies,
    }),
  })
    .then((response) => {
      if (!response.ok) {
        return response.json().then((err) => {
          throw new Error(
            err.message || `HTTP error! status: ${response.status}`
          );
        });
      }
      const contentDisposition = response.headers.get("Content-Disposition");
      let fileName = `exam_models_${new Date()
        .toISOString()
        .replace(/[:.]/g, "")}.zip`;
      if (contentDisposition && contentDisposition.includes("filename=")) {
        fileName = contentDisposition.split("filename=")[1].replace(/"/g, "");
      }
      return response.blob().then((blob) => ({ blob, fileName }));
    })
    .then(({ blob, fileName }) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);
      alert("Exam downloaded successfully!");
    })
    .catch((err) => {
      console.error("Error downloading exam:", err);
      alert(`Error downloading exam: ${err.message}`);
    })
    .finally(() => {
      button.textContent = originalText;
      button.disabled = false;
    });
}

window.onload = populateExams;
