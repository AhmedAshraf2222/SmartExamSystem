const Exams = {
  initialize: function () {
    const examsLabel = document.getElementById("exams-label");
    const examsList = document.getElementById("exams-list");
    const toggleArrow = document.querySelector(".exams-toggle");

    examsLabel.addEventListener("contextmenu", function (e) {
      e.preventDefault();
      Exams.showExamsLabelContextMenu(e);
    });

    toggleArrow.addEventListener("click", (e) => {
      e.stopPropagation();
      Exams.toggleExamsList();
    });

    examsLabel.addEventListener("click", (e) => {
      e.stopPropagation();
      Exams.toggleExamsList();
    });

    Exams.clearContent();
  },

  showExamsLabelContextMenu: function (event) {
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let examsLabelContextMenu = document.getElementById(
      "examsLabelContextMenu"
    );

    if (!examsLabelContextMenu) {
      examsLabelContextMenu = document.createElement("ul");
      examsLabelContextMenu.id = "examsLabelContextMenu";
      examsLabelContextMenu.className = "context-menu";
      examsLabelContextMenu.style.position = "absolute";
      examsLabelContextMenu.style.display = "none";
      examsLabelContextMenu.style.zIndex = "1000";
      examsLabelContextMenu.innerHTML = `<li id="examAddOption">Add Exam</li>`;
      document.body.appendChild(examsLabelContextMenu);
    }

    examsLabelContextMenu.style.left = `${event.pageX}px`;
    examsLabelContextMenu.style.top = `${event.pageY}px`;
    examsLabelContextMenu.style.display = "block";

    const newExamsLabelContextMenu = examsLabelContextMenu.cloneNode(true);
    examsLabelContextMenu.parentNode.replaceChild(
      newExamsLabelContextMenu,
      examsLabelContextMenu
    );

    newExamsLabelContextMenu
      .querySelector("#examAddOption")
      .addEventListener("click", () => {
        Exams.showAddForm();
        newExamsLabelContextMenu.style.display = "none";
      });

    document.addEventListener("click", function hideExamsLabelContextMenu(e) {
      if (!newExamsLabelContextMenu.contains(e.target)) {
        newExamsLabelContextMenu.style.display = "none";
        document.removeEventListener("click", hideExamsLabelContextMenu);
      }
    });
  },

  toggleExamsList: function () {
    const examsList = document.getElementById("exams-list");
    const toggleArrow = document.querySelector(".exams-toggle");

    if (examsList.style.display === "none" || examsList.style.display === "") {
      Exams.fetchExams();
      examsList.style.display = "block";
      toggleArrow.innerHTML = "▲";
      toggleArrow.classList.add("expanded");
    } else {
      examsList.style.display = "none";
      toggleArrow.innerHTML = "▼";
      toggleArrow.classList.remove("expanded");
    }
  },

  fetchExams: function () {
    const examsList = document.getElementById("exams-list");
    examsList.innerHTML =
      '<li class="loading-item"><i class="loading-spinner"></i> Loading...</li>';

    fetch("https://localhost:7181/api/Exams")
      .then((response) => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
      })
      .then((exams) => {
        examsList.innerHTML = "";
        if (exams && exams.length > 0) {
          exams.forEach((exam, index) => {
            const li = document.createElement("li");
            li.className = "exam-item";
            li.innerHTML = `
                <div class="material-content">
                  <span class="material-name">${exam.examName}</span>
                  <span class="material-code">Material: ${
                    exam.materialName || "N/A"
                  }</span>
                </div>
              `;
            li.style.animationDelay = `${index * 0.1}s`;

            li.addEventListener("contextmenu", (e) => {
              e.preventDefault();
              e.stopPropagation();
              Exams.showExamContextMenu(e, exam);
            });

            li.addEventListener("dblclick", (e) => {
              e.preventDefault();
              e.stopPropagation();
              Exams.showDetailsForm(exam);
            });

            li.addEventListener("click", (e) => {
              e.stopPropagation();
            });

            examsList.appendChild(li);
          });
        } else {
          examsList.innerHTML =
            '<li class="no-materials">No exams available</li>';
        }
      })
      .catch((err) => {
        console.error("Error fetching exams:", err);
        examsList.innerHTML = `<li class="error-item">Error loading exams: ${err.message}</li>`;
      });
  },

  fetchMaterials: async function () {
    try {
      const response = await fetch(
        "https://localhost:7181/api/Exams/materials"
      );
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return await response.json();
    } catch (err) {
      console.error("Error fetching materials:", err);
      return [];
    }
  },

  showExamContextMenu: function (event, exam) {
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let examContextMenu = document.getElementById("examContextMenu");

    if (!examContextMenu) {
      examContextMenu = document.createElement("ul");
      examContextMenu.id = "examContextMenu";
      examContextMenu.className = "context-menu";
      examContextMenu.style.position = "absolute";
      examContextMenu.style.display = "none";
      examContextMenu.style.zIndex = "1000";
      examContextMenu.innerHTML = `
          <li id="examDetailsOption">View Details</li>
          <li id="examEditOption">Edit</li>
          <li id="examDeleteOption">Delete</li>
        `;
      document.body.appendChild(examContextMenu);
    }

    examContextMenu.style.left = `${event.pageX}px`;
    examContextMenu.style.top = `${event.pageY}px`;
    examContextMenu.style.display = "block";

    const newExamContextMenu = examContextMenu.cloneNode(true);
    examContextMenu.parentNode.replaceChild(
      newExamContextMenu,
      examContextMenu
    );

    newExamContextMenu
      .querySelector("#examDetailsOption")
      .addEventListener("click", () => {
        Exams.showDetailsForm(exam);
        newExamContextMenu.style.display = "none";
      });

    newExamContextMenu
      .querySelector("#examEditOption")
      .addEventListener("click", () => {
        Exams.showEditForm(exam);
        newExamContextMenu.style.display = "none";
      });

    newExamContextMenu
      .querySelector("#examDeleteOption")
      .addEventListener("click", () => {
        Exams.showDeleteForm(exam);
        newExamContextMenu.style.display = "none";
      });

    document.addEventListener("click", function hideExamContextMenu(e) {
      if (!newExamContextMenu.contains(e.target)) {
        newExamContextMenu.style.display = "none";
        document.removeEventListener("click", hideExamContextMenu);
      }
    });
  },

  showDetailsForm: function (exam) {
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Exam Details</h3>
          <div class="card">
            <div class="card-body">
              <h4 class="card-title">${exam.examName}</h4>
              <p class="card-text"><strong>Material:</strong> ${
                exam.materialName || "N/A"
              }</p>
              <p class="card-text"><strong>Main Degree:</strong> ${
                exam.mainDegree
              }</p>
              <p class="card-text"><strong>Exam Duration:</strong> ${(
                exam.examDuration / 60
              ).toFixed(1)} hours</p>
              <p class="card-text"><strong>Total Problems:</strong> ${
                exam.totalProblems
              }</p>
              <p class="card-text"><strong>Shuffle:</strong> ${
                exam.shuffle ? "Yes" : "No"
              }</p>
              <p class="card-text"><strong>Exam Date:</strong> ${new Date(
                exam.examDate
              ).toLocaleDateString()}</p>
              <p class="card-text"><strong>University:</strong> ${
                exam.universityName || "N/A"
              }</p>
              <p class="card-text"><strong>College:</strong> ${
                exam.collegeName || "N/A"
              }</p>
            </div>
          </div>
          <div class="button-group">
            <button type="button" class="btn-cancel" onclick="Exams.clearContent()">Close</button>
          </div>
        </div>
      `;
  },

  showEditForm: async function (exam) {
    const materials = await Exams.fetchMaterials();
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Edit Exam</h3>
          <form id="editExamForm" class="material-form">
            <input type="hidden" id="ExamId" value="${exam.examId}" />
            <div class="input-item">
              <label for="ExamName">Exam Name</label>
              <input type="text" id="ExamName" value="${
                exam.examName
              }" required maxlength="40" />
            </div>
            <div class="input-item">
              <label for="MaterialId">Material</label>
              <select id="MaterialId" required>
                <option value="" disabled>Select a material</option>
                ${materials
                  .map(
                    (material) =>
                      `<option value="${material.materialId}" ${
                        material.materialId === exam.materialId
                          ? "selected"
                          : ""
                      }>${material.materialName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="input-item">
              <label for="MainDegree">Main Degree</label>
              <input type="text" id="MainDegree" value="${
                exam.mainDegree
              }" required pattern="\\d+" title="Please enter a positive number" />
            </div>
            <div class="input-item">
              <label for="TotalProblems">Total Problems</label>
              <input type="text" id="TotalProblems" value="${
                exam.totalProblems
              }" required pattern="\\d+" title="Please enter a positive number" />
            </div>
            <div class="input-item">
              <label for="ExamDuration">Exam Duration (hours)</label>
              <input type="text" id="ExamDuration" value="${(
                exam.examDuration / 60
              ).toFixed(
                1
              )}" required pattern="\\d+(\\.\\d{1,2})?" title="Please enter a positive number (e.g., 1 or 1.5)" />
            </div>
            <div class="input-item">
              <label for="ExamDate">Exam Date</label>
              <input type="date" id="ExamDate" value="${new Date(exam.examDate)
                .toISOString()
                .slice(0, 10)}" required />
            </div>
            <div class="input-item">
              <label for="UniversityName">University Name</label>
              <input type="text" id="UniversityName" value="${
                exam.universityName || ""
              }" maxlength="100" />
            </div>
            <div class="input-item">
              <label for="CollegeName">College Name</label>
              <input type="text" id="CollegeName" value="${
                exam.collegeName || ""
              }" maxlength="100" />
            </div>
           <div class="input-item checkbox">
                <label for="Shuffle">Shuffle</label>
                <input type="checkbox" id="Shuffle" ${
                  exam.shuffle ? "checked" : ""
                } 
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Changes</button>
              <button type="button" class="btn-cancel" onclick="Exams.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;

    document
      .getElementById("editExamForm")
      .addEventListener("submit", function (e) {
        e.preventDefault();

        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
        saveBtn.disabled = true;

        const examDurationHours = parseFloat(
          document.getElementById("ExamDuration").value
        );
        const examDurationMinutes = Math.round(examDurationHours * 60);

        const data = {
          examName: document.getElementById("ExamName").value,
          materialId: parseInt(document.getElementById("MaterialId").value),
          mainDegree: parseInt(document.getElementById("MainDegree").value),
          totalProblems: parseInt(
            document.getElementById("TotalProblems").value
          ),
          shuffle: document.getElementById("Shuffle").checked,
          examDuration: examDurationMinutes,
          examDate: new Date(
            document.getElementById("ExamDate").value
          ).toISOString(),
          universityName:
            document.getElementById("UniversityName").value || null,
          collegeName: document.getElementById("CollegeName").value || null,
        };

        fetch(`https://localhost:7181/api/Exams/${exam.examId}`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(data),
        })
          .then((res) => {
            if (!res.ok) {
              return res.json().then((error) => {
                throw new Error(
                  error.message || `HTTP error! status: ${res.status}`
                );
              });
            }
            return res.json();
          })
          .then((response) => {
            Exams.showSuccessMessage(
              response.message || "Exam updated successfully."
            );
            Exams.fetchExams();
            Exams.clearContent();
          })
          .catch((err) => {
            console.error("Error:", err);
            Exams.showErrorMessage(
              err.message || "Error updating exam. Please try again."
            );
          })
          .finally(() => {
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
          });
      });
  },

  showDeleteForm: function (exam) {
    const content = document.getElementById("content");
    content.innerHTML = `
      <div class="content-display">
        <h3 class="content-title">Delete Exam</h3>
        <p class="delete-message">
          Are you sure you want to delete the exam 
          <strong>${exam.examName}</strong>?
        </p>
        <div class="button-group">
          <button type="button" class="btn-delete" onclick="Exams.deleteExam(${exam.examId})">Delete</button>
          <button type="button" class="btn-cancel" onclick="Exams.clearContent()">Cancel</button>
        </div>
      </div>
    `;
  },

  deleteExam: function (examId) {
    const deleteBtn = document.querySelector(".btn-delete");
    const originalText = deleteBtn.textContent;
    deleteBtn.innerHTML = '<i class="loading-spinner"></i> Deleting...';
    deleteBtn.disabled = true;

    fetch(`https://localhost:7181/api/Exams/${examId}`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
      },
    })
      .then((res) => {
        if (!res.ok) {
          return res.json().then((error) => {
            throw new Error(
              error.message || `HTTP error! status: ${res.status}`
            );
          });
        }
        return res.json();
      })
      .then((response) => {
        Exams.showSuccessMessage(
          response.message || "Exam deleted successfully."
        );
        Exams.fetchExams();
        Exams.clearContent();
      })
      .catch((err) => {
        console.error("Error:", err);
        Exams.showErrorMessage(
          err.message || "Error deleting exam. Please try again."
        );
      })
      .finally(() => {
        deleteBtn.textContent = originalText;
        deleteBtn.disabled = false;
      });
  },

  showAddForm: async function () {
    const materials = await Exams.fetchMaterials();
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Add New Exam</h3>
          <form id="examForm" class="material-form">
            <div class="input-item">
              <label for="ExamName">Exam Name</label>
              <input type="text" id="ExamName" placeholder="Enter exam name" required maxlength="40" />
            </div>
            <div class="input-item">
              <label for="MaterialId">Material</label>
              <select id="MaterialId" required>
                <option value="" disabled selected>Select a material</option>
                ${materials
                  .map(
                    (material) =>
                      `<option value="${material.materialId}">${material.materialName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="input-item">
              <label for="MainDegree">Main Degree</label>
              <input type="text" id="MainDegree" placeholder="Enter main degree" required pattern="\\d+" title="Please enter a positive number" />
            </div>
            <div class="input-item">
              <label for="TotalProblems">Total Problems</label>
              <input type="text" id="TotalProblems" placeholder="Enter total problems" required pattern="\\d+" title="Please enter a positive number" />
            </div>
            <div class="input-item">
              <label for="ExamDuration">Exam Duration (hours)</label>
              <input type="text" id="ExamDuration" placeholder="Enter exam duration (e.g., 1 or 1.5)" required pattern="\\d+(\\.\\d{1,2})?" title="Please enter a positive number (e.g., 1 or 1.5)" />
            </div>
            <div class="input-item">
              <label for="ExamDate">Exam Date</label>
              <input type="date" id="ExamDate" required />
            </div>
            <div class="input-item">
              <label for="UniversityName">University Name</label>
              <input type="text" id="UniversityName" placeholder="Enter university name" maxlength="100" />
            </div>
            <div class="input-item">
              <label for="CollegeName">College Name</label>
              <input type="text" id="CollegeName" placeholder="Enter college name" maxlength="100" />
            </div>
              <div class="input-item checkbox">       
              <label for="Shuffle">Shuffle</label>
              <input type="checkbox" id="Shuffle" />
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Exam</button>
              <button type="button" class="btn-cancel" onclick="Exams.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;

    document
      .getElementById("examForm")
      .addEventListener("submit", async function (e) {
        e.preventDefault();

        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
        saveBtn.disabled = true;

        const examDurationHours = parseFloat(
          document.getElementById("ExamDuration").value
        );
        const examDurationMinutes = Math.round(examDurationHours * 60);

        const data = {
          examName: document.getElementById("ExamName").value,
          materialId: parseInt(document.getElementById("MaterialId").value),
          mainDegree: parseInt(document.getElementById("MainDegree").value),
          totalProblems: parseInt(
            document.getElementById("TotalProblems").value
          ),
          shuffle: document.getElementById("Shuffle").checked,
          examDuration: examDurationMinutes,
          examDate: new Date(
            document.getElementById("ExamDate").value
          ).toISOString(),
          universityName:
            document.getElementById("UniversityName").value || null,
          collegeName: document.getElementById("CollegeName").value || null,
        };

        try {
          const response = await fetch("https://localhost:7181/api/Exams", {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify(data),
          });

          if (!response.ok) {
            const error = await response.json();
            throw new Error(
              error.message || `HTTP error! status: ${response.status}`
            );
          }

          const result = await response.json();
          const selectedMaterial = materials.find(
            (m) =>
              m.materialId ===
              parseInt(document.getElementById("MaterialId").value)
          );

          Exams.showSuccessMessage(
            result.message || "Exam added successfully."
          );
          Exams.addExamToTree({
            examId: result.examId,
            examName: document.getElementById("ExamName").value,
            materialName: selectedMaterial
              ? selectedMaterial.materialName
              : "N/A",
            materialId: parseInt(document.getElementById("MaterialId").value),
            mainDegree: parseInt(document.getElementById("MainDegree").value),
            totalProblems: parseInt(
              document.getElementById("TotalProblems").value
            ),
            shuffle: document.getElementById("Shuffle").checked,
            examDuration: examDurationMinutes,
            examDate: new Date(
              document.getElementById("ExamDate").value
            ).toISOString(),
            universityName:
              document.getElementById("UniversityName").value || null,
            collegeName: document.getElementById("CollegeName").value || null,
          });

          Exams.clearContent();
        } catch (err) {
          console.error("Error:", err);
          Exams.showErrorMessage(
            err.message || "Error adding exam. Please try again."
          );
        } finally {
          saveBtn.textContent = originalText;
          saveBtn.disabled = false;
        }
      });
  },

  addExamToTree: function (exam) {
    const examsList = document.getElementById("exams-list");
    const li = document.createElement("li");
    li.className = "exam-item new-item";
    li.innerHTML = `
        <div class="material-content">
          <span class="material-name">${exam.examName}</span>
          <span class="material-code">Material: ${
            exam.materialName || "N/A"
          }</span>
        </div>
      `;

    li.addEventListener("contextmenu", (e) => {
      e.preventDefault();
      e.stopPropagation();
      Exams.showExamContextMenu(e, exam);
    });

    li.addEventListener("dblclick", (e) => {
      e.preventDefault();
      e.stopPropagation();
      Exams.showDetailsForm(exam);
    });

    li.addEventListener("click", (e) => {
      e.stopPropagation();
    });

    examsList.appendChild(li);
    setTimeout(() => {
      li.classList.remove("new-item");
    }, 1000);
  },

  clearContent: function () {
    document.getElementById("content").innerHTML = `
        <div class="welcome-message">
          <h2>Welcome to Exams Management System</h2>
          <p>Select an item from the sidebar to begin</p>
        </div>
      `;
  },

  showSuccessMessage: function (message) {
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <div class="message-container success">
            <div class="message-icon">✓</div>
            <h3>${message}</h3>
            <button onclick="Exams.clearContent()" class="btn-back">Return to Home</button>
          </div>
        </div>
      `;
  },

  showErrorMessage: function (message) {
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <div class="message-container error">
            <div class="message-icon">✗</div>
            <h3>${message}</h3>
            <button onclick="Exams.clearContent()" class="btn-back">Return to Home</button>
          </div>
        </div>
      `;
  },
};

document.addEventListener("DOMContentLoaded", Exams.initialize);
