const Problems = {
  initialize: function () {
    const problemsLabel = document.getElementById("problems-label");
    const problemsList = document.getElementById("problems-list");
    const toggleArrow = document.querySelector(".problems-toggle");

    problemsLabel.addEventListener("contextmenu", function (e) {
      e.preventDefault();
      Problems.showProblemsLabelContextMenu(e);
    });

    toggleArrow.addEventListener("click", (e) => {
      e.stopPropagation();
      Problems.toggleProblemsList();
    });

    problemsLabel.addEventListener("click", (e) => {
      e.stopPropagation();
      Problems.toggleProblemsList();
    });

    Problems.clearContent();
  },

  showProblemsLabelContextMenu: function (event) {
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let problemsLabelContextMenu = document.getElementById(
      "problemsLabelContextMenu"
    );

    if (!problemsLabelContextMenu) {
      problemsLabelContextMenu = document.createElement("ul");
      problemsLabelContextMenu.id = "problemsLabelContextMenu";
      problemsLabelContextMenu.className = "context-menu";
      problemsLabelContextMenu.style.position = "absolute";
      problemsLabelContextMenu.style.display = "none";
      problemsLabelContextMenu.style.zIndex = "1000";
      problemsLabelContextMenu.innerHTML = `<li id="problemAddOption">Add Problem</li>`;
      document.body.appendChild(problemsLabelContextMenu);
    }

    problemsLabelContextMenu.style.left = `${event.pageX}px`;
    problemsLabelContextMenu.style.top = `${event.pageY}px`;
    problemsLabelContextMenu.style.display = "block";

    const newProblemsLabelContextMenu =
      problemsLabelContextMenu.cloneNode(true);
    problemsLabelContextMenu.parentNode.replaceChild(
      newProblemsLabelContextMenu,
      problemsLabelContextMenu
    );

    newProblemsLabelContextMenu
      .querySelector("#problemAddOption")
      .addEventListener("click", () => {
        Problems.showAddForm();
        newProblemsLabelContextMenu.style.display = "none";
      });

    document.addEventListener(
      "click",
      function hideProblemsLabelContextMenu(e) {
        if (!newProblemsLabelContextMenu.contains(e.target)) {
          newProblemsLabelContextMenu.style.display = "none";
          document.removeEventListener("click", hideProblemsLabelContextMenu);
        }
      }
    );
  },

  toggleProblemsList: function () {
    const problemsList = document.getElementById("problems-list");
    const toggleArrow = document.querySelector(".problems-toggle");

    if (
      problemsList.style.display === "none" ||
      problemsList.style.display === ""
    ) {
      Problems.fetchProblems();
      problemsList.style.display = "block";
      toggleArrow.innerHTML = "▲";
      toggleArrow.classList.add("expanded");
    } else {
      problemsList.style.display = "none";
      toggleArrow.innerHTML = "▼";
      toggleArrow.classList.remove("expanded");
    }
  },

  fetchProblems: function () {
    const problemsList = document.getElementById("problems-list");
    problemsList.innerHTML =
      '<li class="loading-item"><i class="loading-spinner"></i> Loading...</li>';

    fetch("https://localhost:7181/api/Problems")
      .then((response) => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
      })
      .then((problems) => {
        problemsList.innerHTML = "";
        if (problems && problems.length > 0) {
          problems.forEach((problem, index) => {
            const li = document.createElement("li");
            li.className = "problem-item";
            li.innerHTML = `
                <div class="material-content">
                  <span class="material-name">${problem.problemName}</span>
                  <span class="material-code">Group: ${
                    problem.groupName || "N/A"
                  }</span>
                </div>
              `;
            li.style.animationDelay = `${index * 0.1}s`;

            li.addEventListener("contextmenu", (e) => {
              e.preventDefault();
              e.stopPropagation();
              Problems.showProblemContextMenu(e, problem);
            });

            li.addEventListener("dblclick", (e) => {
              e.preventDefault();
              e.stopPropagation();
              Problems.showDetailsForm(problem);
            });

            li.addEventListener("click", (e) => {
              e.stopPropagation();
            });

            problemsList.appendChild(li);
          });
        } else {
          problemsList.innerHTML =
            '<li class="no-materials">No problems available</li>';
        }
      })
      .catch((err) => {
        console.error("Error fetching problems:", err);
        problemsList.innerHTML = `<li class="error-item">Error loading problems: ${err.message}</li>`;
      });
  },

  fetchGroups: async function () {
    try {
      const response = await fetch(
        "https://localhost:7181/api/Problems/groups"
      );
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return await response.json();
    } catch (err) {
      console.error("Error fetching groups:", err);
      return [];
    }
  },

  fetchChoices: async function (problemId) {
    try {
      console.log(`Fetching choices for problemId: ${problemId}`);
      const response = await fetch(
        `https://localhost:7181/api/Problems/ProblemChoices/${problemId}`
      );
      console.log(`Response status: ${response.status}, OK: ${response.ok}`);
      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        console.error("Error response data:", errorData);
        throw new Error(
          errorData.message || `HTTP error! status: ${response.status}`
        );
      }
      const choices = await response.json();
      console.log("Fetched choices for problem", problemId, ":", choices);
      return Array.isArray(choices) ? choices : [];
    } catch (err) {
      console.error("Error fetching choices for problem", problemId, ":", err);
      return [];
    }
  },

  showProblemContextMenu: function (event, problem) {
    console.log("Showing context menu for problem:", problem);
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let problemContextMenu = document.getElementById("problemContextMenu");

    if (!problemContextMenu) {
      problemContextMenu = document.createElement("ul");
      problemContextMenu.id = "problemContextMenu";
      problemContextMenu.className = "context-menu";
      problemContextMenu.style.position = "absolute";
      problemContextMenu.style.display = "none";
      problemContextMenu.style.zIndex = "1000";
      problemContextMenu.innerHTML = `
          <li id="problemDetailsOption">View Details</li>
          <li id="problemEditOption">Edit</li>
          <li id="problemDeleteOption">Delete</li>
        `;
      document.body.appendChild(problemContextMenu);
    }

    problemContextMenu.style.left = `${event.pageX}px`;
    problemContextMenu.style.top = `${event.pageY}px`;
    problemContextMenu.style.display = "block";

    const newProblemContextMenu = problemContextMenu.cloneNode(true);
    problemContextMenu.parentNode.replaceChild(
      newProblemContextMenu,
      problemContextMenu
    );

    newProblemContextMenu
      .querySelector("#problemDetailsOption")
      .addEventListener("click", () => {
        Problems.showDetailsForm(problem);
        newProblemContextMenu.style.display = "none";
      });

    newProblemContextMenu
      .querySelector("#problemEditOption")
      .addEventListener("click", () => {
        Problems.showEditForm(problem);
        newProblemContextMenu.style.display = "none";
      });

    newProblemContextMenu
      .querySelector("#problemDeleteOption")
      .addEventListener("click", () => {
        Problems.showDeleteForm(problem);
        newProblemContextMenu.style.display = "none";
      });

    document.addEventListener("click", function hideProblemContextMenu(e) {
      if (!newProblemContextMenu.contains(e.target)) {
        newProblemContextMenu.style.display = "none";
        document.removeEventListener("click", hideProblemContextMenu);
      }
    });
  },

  showDetailsForm: async function (problem) {
    try {
      const response = await fetch(
        `https://localhost:7181/api/Problems/${problem.problemId}`
      );
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      const problemData = await response.json();

      const content = document.getElementById("content");
      if (!content) return;

      let choicesHtml = "";
      if (problemData.choices && problemData.choices.length > 0) {
        problemData.choices.forEach((choice, index) => {
          const isCorrect = index + 1 === problemData.rightAnswer;
          choicesHtml += `
                    <div class="choice-item ${
                      isCorrect ? "correct-choice" : ""
                    }">
                        <span class="choice-number">Choice ${index + 1}:</span>
                        <span class="choice-text">${
                          choice.choices || "N/A"
                        }</span>
                        ${
                          choice.choiceImagePath
                            ? `<img src="${choice.choiceImagePath}" class="choice-image" style="max-width: 100px;">`
                            : ""
                        }
                        ${
                          isCorrect
                            ? '<span class="correct-badge">(Correct)</span>'
                            : ""
                        }
                    </div>
                `;
        });
      } else {
        choicesHtml = '<div class="no-choices">No choices available</div>';
      }

      content.innerHTML = `
            <div class="content-display">
                <h3 class="content-title">Problem Details</h3>
                <div class="material-details">
                    <div class="detail-item">
                        <span class="detail-label">Problem Name:</span>
                        <span class="detail-value">${
                          problemData.problemName || "N/A"
                        }</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Group:</span>
                        <span class="detail-value">${
                          problemData.groupName || "N/A"
                        }</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Main Degree:</span>
                        <span class="detail-value">${
                          problemData.mainDegree || "N/A"
                        }</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Problem Header:</span>
                        <span class="detail-value">${
                          problemData.problemHeader || "N/A"
                        }</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Shuffle:</span>
                        <span class="detail-value">${
                          problemData.shuffle ? "Yes" : "No"
                        }</span>
                    </div>
                    ${
                      problemData.problemImagePath
                        ? `
                    <div class="detail-item">
                        <span class="detail-label">Problem Image:</span>
                        <img src="${problemData.problemImagePath}" style="max-width: 300px;">
                    </div>
                    `
                        : ""
                    }
                    <div class="detail-item">
                        <span class="detail-label">Right Answer:</span>
                        <span class="detail-value">Choice ${
                          problemData.rightAnswer || "N/A"
                        }</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">Choices:</span>
                        <div class="choices-list">
                            ${choicesHtml}
                        </div>
                    </div>
                </div>
                <div class="button-group">
                    <button type="button" class="btn-cancel" onclick="Problems.clearContent()">Close</button>
                </div>
            </div>
        `;
    } catch (err) {
      console.error("Error in showDetailsForm:", err);
      Problems.showErrorToast("Failed to load problem details");
    }
  },

  generateChoices: function (
    choicesCount,
    existingChoices = [],
    rightAnswer = 1
  ) {
    console.log("Generating choices:", {
      choicesCount,
      existingChoices,
      rightAnswer,
    });
    const choicesContainer = document.getElementById("choicesContainer");
    const rightAnswerContainer = document.getElementById(
      "rightAnswerContainer"
    );

    if (!choicesContainer || !rightAnswerContainer) {
      console.error("Choices container or right answer container not found");
      return;
    }

    choicesContainer.innerHTML = "";
    rightAnswerContainer.innerHTML = "";

    for (let i = 0; i < choicesCount; i++) {
      const existingChoice = existingChoices[i] || {};

      const choiceBlock = `
        <div class="form-group">
          <label for="choice_${i}">Choice ${i + 1}</label>
          <input id="choice_${i}" name="ProblemChoices[${i}].Choices" class="form-control" value="${
        existingChoice.choices || ""
      }" required maxlength="500" />
          <input type="hidden" name="ProblemChoices[${i}].ChoiceId" value="${
        existingChoice.choiceId || ""
      }" />
          ${
            existingChoice.choiceImagePath
              ? `
            <div class="mb-2">
              <img src="${existingChoice.choiceImagePath}" alt="Current Choice Image" class="img-thumbnail" style="max-width: 100px;" />
              <div class="form-check mt-2">
                <input type="checkbox" id="removeChoiceImage_${i}" name="removeChoiceImage_${i}" class="form-check-input" />
                <label for="removeChoiceImage_${i}" class="form-check-label">Remove current image</label>
              </div>
            </div>
          `
              : ""
          }
          <div class="form-check mt-2">
            <input type="checkbox" class="form-check-input" id="choiceImageCheck_${i}" onchange="Problems.toggleChoiceImage(${i})" />
            <label class="form-check-label" for="choiceImageCheck_${i}">Is there an image for this choice?</label>
          </div>
          <div id="choiceImageSection_${i}" style="display: none;" class="mt-2">
            <label for="choiceImage_${i}">Upload Choice Image</label>
            <input type="file" class="form-control" id="choiceImage_${i}" name="choiceImages[${i}]" accept=".jpg,.jpeg,.png" />
          </div>
        </div>`;

      choicesContainer.innerHTML += choiceBlock;

      const radioInput = `
        <div class="form-check form-check-inline" style="margin-right: 10px;">
          <input type="radio" name="RightAnswer" id="rightAnswer_${i}" value="${
        i + 1
      }" class="form-check-input" ${i + 1 === rightAnswer ? "checked" : ""} ${
        i === 0 ? "required" : ""
      } />
          <label class="form-check-label" for="rightAnswer_${i}">Choice ${
        i + 1
      }</label>
        </div>`;
      rightAnswerContainer.innerHTML += radioInput;
    }
  },

  showEditForm: async function (problem) {
    console.log("Starting showEditForm for problem:", problem);
    try {
      const [groups, choices] = await Promise.all([
        Problems.fetchGroups(),
        Problems.fetchChoices(problem.problemId),
      ]);

      console.log("Fetched groups and choices:", { groups, choices });

      const content = document.getElementById("content");
      if (!content) throw new Error("Content element not found");

      content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Edit Problem</h3>
          <form id="editProblemForm" class="material-form" enctype="multipart/form-data">
            <input type="hidden" id="ProblemId" value="${problem.problemId}" />
            <div class="input-item">
              <label for="GroupId">Group</label>
              <select id="GroupId" name="GroupId" required>
                <option value="">Select a group</option>
                ${groups
                  .map(
                    (group) =>
                      `<option value="${group.groupId}" ${
                        group.groupId === problem.groupId ? "selected" : ""
                      }>${group.groupName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="input-item">
              <label for="ProblemName">Problem Name</label>
              <input type="text" id="ProblemName" name="ProblemName" value="${
                problem.problemName
              }" required maxlength="40" />
            </div>
            <div class="input-item">
              <label for="ProblemHeader">Problem Header</label>
              <textarea id="ProblemHeader" name="ProblemHeader" maxlength="1000">${
                problem.problemHeader || ""
              }</textarea>
            </div>
            <div class="input-item checkbox">
              <label for="imageCheck">Is there an image for this problem?</label>
              <input type="checkbox" id="imageCheck" name="imageCheck" class="form-check-input" ${
                problem.problemImagePath ? "checked" : ""
              } onchange="Problems.toggleImageUpload()" />
            </div>
            <div id="imageUploadSection" style="display: ${
              problem.problemImagePath ? "block" : "none"
            };">
              <div class="input-item">
                <label for="ProblemImage">Upload Image</label>
                ${
                  problem.problemImagePath
                    ? `
                <div class="mb-2">
                  <img src="${problem.problemImagePath}" alt="Current Image" class="img-thumbnail" style="max-width: 200px;" />
                  <div class="form-check mt-2">
                    <input type="checkbox" id="removeImage" name="removeImage" class="form-check-input" />
                    <label for="removeImage" class="form-check-label">Remove current image</label>
                  </div>
                </div>
              `
                    : ""
                }
                <input type="file" id="ProblemImage" name="problemImage" accept=".jpg,.jpeg,.png" />
              </div>
            </div>
            <div class="input-item">
              <label for="ChoicesNumber">Number of Choices</label>
              <select id="ChoicesNumber" name="ChoicesNumber" required>
                ${[2, 3, 4, 5, 6, 7, 8, 9, 10]
                  .map(
                    (i) =>
                      `<option value="${i}" ${
                        choices.length === i ? "selected" : ""
                      }>${i}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div id="choicesContainer" class="input-item"></div>
            <div class="input-item">
              <label for="RightAnswer">Right Answer</label>
              <div id="rightAnswerContainer"></div>
            </div>
            <div class="input-item checkbox">
              <label for="Shuffle">Shuffle Choices</label>
              <input type="checkbox" id="Shuffle" name="Shuffle" ${
                problem.shuffle ? "checked" : ""
              } />
            </div>
            <div class="input-item">
              <label for="MainDegree">Main Degree</label>
              <input type="number" id="MainDegree" name="MainDegree" value="${
                problem.mainDegree
              }" required min="1" max="100" />
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Changes</button>
              <button type="button" class="btn-cancel" onclick="Problems.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;

      if (!choices || choices.length === 0) {
        console.warn("No choices found for problem", problem.problemId);
        Problems.showErrorToast(
          "No choices found for this problem. Please add choices."
        );
        Problems.generateChoices(2, [], problem.rightAnswer || 1);
      } else {
        Problems.generateChoices(choices.length, choices, problem.rightAnswer);
      }

      document
        .getElementById("ChoicesNumber")
        .addEventListener("change", () => {
          const newCount = parseInt(
            document.getElementById("ChoicesNumber").value
          );
          Problems.generateChoices(
            newCount,
            choices.slice(0, newCount),
            problem.rightAnswer || 1
          );
        });

      document
        .getElementById("editProblemForm")
        .addEventListener("submit", async function (e) {
          e.preventDefault();

          const saveBtn = document.querySelector(".btn-save");
          const originalText = saveBtn.textContent;
          saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
          saveBtn.disabled = true;

          const formData = new FormData();
          formData.append("ProblemId", problem.problemId);
          formData.append("GroupId", document.getElementById("GroupId").value);
          formData.append(
            "ProblemName",
            document.getElementById("ProblemName").value
          );
          formData.append(
            "ProblemHeader",
            document.getElementById("ProblemHeader").value
          );
          formData.append(
            "RightAnswer",
            document.querySelector('input[name="RightAnswer"]:checked').value
          );
          formData.append(
            "Shuffle",
            document.getElementById("Shuffle").checked
          );
          formData.append(
            "MainDegree",
            document.getElementById("MainDegree").value
          );

          const problemImage = document.getElementById("ProblemImage").files[0];
          if (problemImage) {
            formData.append("ProblemImage", problemImage);
          }
          if (document.getElementById("removeImage")?.checked) {
            formData.append("removeImage", "true");
          }

          const choicesInputs = document.querySelectorAll(
            'input[name^="ProblemChoices"]'
          );
          const choiceData = [];
          for (let i = 0; i < choicesInputs.length; i += 2) {
            const choiceText = choicesInputs[i].value;
            const choiceId = choicesInputs[i + 1].value;
            const choiceImage = document.getElementById(`choiceImage_${i / 2}`)
              ?.files[0];
            const removeChoiceImage = document.getElementById(
              `removeChoiceImage_${i / 2}`
            )?.checked;
            choiceData.push({
              choiceId: choiceId || null,
              choices: choiceText,
              choiceImage,
              removeChoiceImage,
              unitOrder: i / 2 + 1,
            });
          }

          try {
            const problemResponse = await fetch(
              `https://localhost:7181/api/Problems/${problem.problemId}`,
              {
                method: "PUT",
                body: formData,
              }
            );

            if (!problemResponse.ok) {
              const errorData = await problemResponse.json();
              throw new Error(
                errorData.message ||
                  `HTTP error! status: ${problemResponse.status}`
              );
            }

            for (const choice of choiceData) {
              const choiceFormData = new FormData();
              choiceFormData.append("Choices", choice.choices);
              choiceFormData.append("UnitOrder", choice.unitOrder);
              choiceFormData.append("ProblemId", problem.problemId);
              if (choice.choiceImage) {
                choiceFormData.append("ChoiceImage", choice.choiceImage);
              }
              if (choice.removeChoiceImage) {
                choiceFormData.append("removeChoiceImage", "true");
              }

              if (choice.choiceId) {
                await fetch(
                  `https://localhost:7181/api/ProblemChoices/${choice.choiceId}`,
                  {
                    method: "PUT",
                    body: choiceFormData,
                  }
                );
              } else {
                await fetch("https://localhost:7181/api/ProblemChoices", {
                  method: "POST",
                  body: choiceFormData,
                });
              }
            }

            Problems.showSuccessMessage("Problem updated successfully.");
            Problems.fetchProblems();
            Problems.clearContent();
          } catch (err) {
            console.error("Error:", err);
            Problems.showErrorMessage(
              err.message || "Error updating problem. Please try again."
            );
          } finally {
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
          }
        });
    } catch (err) {
      console.error("Error in showEditForm:", err);
      Problems.showErrorToast("Failed to load edit form. Please try again.");
    }
  },

  showDeleteForm: function (problem) {
    const content = document.getElementById("content");
    content.innerHTML = `
      <div class="content-display">
        <h3 class="content-title">Delete Problem</h3>
        <p class="delete-message">
          Are you sure you want to delete the problem 
          <strong>${problem.problemName}</strong>?
        </p>
        <div class="button-group">
          <button type="button" class="btn-delete" onclick="Problems.deleteProblem(${problem.problemId})">Delete</button>
          <button type="button" class="btn-cancel" onclick="Problems.clearContent()">Cancel</button>
        </div>
      </div>
    `;
  },

  deleteProblem: function (problemId) {
    const deleteBtn = document.querySelector(".btn-delete");
    const originalText = deleteBtn.textContent;
    deleteBtn.innerHTML = '<i class="loading-spinner"></i> Deleting...';
    deleteBtn.disabled = true;

    fetch(`https://localhost:7181/api/Problems/${problemId}`, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
      },
    })
      .then((res) => {
        if (!res.ok) {
          throw new Error(`HTTP error! status: ${res.status}`);
        }
        return res.json();
      })
      .then((response) => {
        Problems.showSuccessMessage(response.message);
        Problems.fetchProblems();
        Problems.clearContent();
      })
      .catch((err) => {
        console.error("Error:", err);
        Problems.showErrorMessage("Error deleting problem. Please try again.");
      })
      .finally(() => {
        deleteBtn.textContent = originalText;
        deleteBtn.disabled = false;
      });
  },

  showAddForm: async function () {
    const groups = await Problems.fetchGroups();
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Add New Problem</h3>
          <form id="problemForm" class="material-form" enctype="multipart/form-data">
            <div class="input-item">
              <label for="GroupId">Group</label>
              <select id="GroupId" name="GroupId" required>
                <option value="">Select a group</option>
                ${groups
                  .map(
                    (group) =>
                      `<option value="${group.groupId}">${group.groupName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="input-item">
              <label for="ProblemName">Problem Name</label>
              <input type="text" id="ProblemName" name="ProblemName" placeholder="Enter problem name" required maxlength="40" />
            </div>
            <div class="input-item">
              <label for="ProblemHeader">Problem Header</label>
              <textarea id="ProblemHeader" name="ProblemHeader" placeholder="Enter problem header" maxlength="1000"></textarea>
            </div>
            <div class="input-item checkbox">
              <label for="imageCheck">Is there an image for this problem?</label>
              <input type="checkbox" id="imageCheck" name="imageCheck" class="form-check-input" onchange="Problems.toggleImageUpload()" />
            </div>
            <div id="imageUploadSection" style="display: none;">
              <div class="input-item">
                <label for="ProblemImage">Upload Image</label>
                <input type="file" id="ProblemImage" name="problemImage" accept=".jpg,.jpeg,.png" />
              </div>
            </div>
            <div class="input-item">
              <label for="ChoicesNumber">Number of Choices</label>
              <select id="ChoicesNumber" name="ChoicesNumber" required>
                ${[2, 3, 4, 5, 6, 7, 8, 9, 10]
                  .map((i) => `<option value="${i}">${i}</option>`)
                  .join("")}
              </select>
            </div>
            <div id="choicesContainer" class="input-item"></div>
            <div class="input-item">
              <label for="RightAnswer">Right Answer</label>
              <div id="rightAnswerContainer"></div>
            </div>
            <div class="input-item checkbox">
              <label for="Shuffle">Shuffle Choices</label>
              <input type="checkbox" id="Shuffle" name="Shuffle" />
            </div>
            <div class="input-item">
              <label for="MainDegree">Main Degree</label>
              <input type="number" id="MainDegree" name="MainDegree" placeholder="Enter main degree" required min="1" max="100" />
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Problem</button>
              <button type="button" class="btn-cancel" onclick="Problems.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;

    function generateChoices(choicesCount) {
      const choicesContainer = document.getElementById("choicesContainer");
      const rightAnswerContainer = document.getElementById(
        "rightAnswerContainer"
      );
      choicesContainer.innerHTML = "";
      rightAnswerContainer.innerHTML = "";

      for (let i = 0; i < choicesCount; i++) {
        const choiceBlock = `
          <div class="form-group">
            <label for="choice_${i}">Choice ${i + 1}</label>
            <input id="choice_${i}" name="ProblemChoices[${i}].Choices" class="form-control" required maxlength="500" />
            <div class="form-check mt-2">
              <input type="checkbox" class="form-check-input" id="choiceImageCheck_${i}" onchange="Problems.toggleChoiceImage(${i})" />
              <label class="form-check-label" for="choiceImageCheck_${i}">Is there an image for this choice?</label>
            </div>
            <div id="choiceImageSection_${i}" style="display: none;" class="mt-2">
              <label for="choiceImage_${i}">Upload Choice Image</label>
              <input type="file" class="form-control" id="choiceImage_${i}" name="choiceImages[${i}]" accept=".jpg,.jpeg,.png" />
            </div>
          </div>`;
        choicesContainer.innerHTML += choiceBlock;

        const radioInput = `
          <div class="form-check form-check-inline" style="margin-right: 10px;">
            <input type="radio" name="RightAnswer" id="rightAnswer_${i}" value="${
          i + 1
        }" class="form-check-input" ${i === 0 ? "checked required" : ""} />
            <label class="form-check-label" for="rightAnswer_${i}">Choice ${
          i + 1
        }</label>
          </div>`;
        rightAnswerContainer.innerHTML += radioInput;
      }
    }

    document.getElementById("ChoicesNumber").addEventListener("change", () => {
      generateChoices(parseInt(document.getElementById("ChoicesNumber").value));
    });

    generateChoices(2);

    document
      .getElementById("problemForm")
      .addEventListener("submit", async function (e) {
        e.preventDefault();

        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
        saveBtn.disabled = true;

        const formData = new FormData();
        formData.append("GroupId", document.getElementById("GroupId").value);
        formData.append(
          "ProblemName",
          document.getElementById("ProblemName").value
        );
        formData.append(
          "ProblemHeader",
          document.getElementById("ProblemHeader").value
        );
        formData.append(
          "RightAnswer",
          document.querySelector('input[name="RightAnswer"]:checked').value
        );
        formData.append("Shuffle", document.getElementById("Shuffle").checked);
        formData.append(
          "MainDegree",
          document.getElementById("MainDegree").value
        );
        const problemImage = document.getElementById("ProblemImage")?.files[0];
        if (problemImage) {
          formData.append("ProblemImage", problemImage);
        }

        const choicesInputs = document.querySelectorAll(
          'input[name^="ProblemChoices"]'
        );
        const choiceData = [];
        for (let i = 0; i < choicesInputs.length; i++) {
          const choiceText = choicesInputs[i].value;
          const choiceImage = document.getElementById(`choiceImage_${i}`)
            ?.files[0];
          choiceData.push({
            choices: choiceText,
            choiceImage,
            unitOrder: i + 1,
          });
        }

        try {
          const problemResponse = await fetch(
            "https://localhost:7181/api/Problems",
            {
              method: "POST",
              body: formData,
            }
          );

          if (!problemResponse.ok) {
            const errorData = await problemResponse.json();
            throw new Error(
              errorData.message ||
                `HTTP error! status: ${problemResponse.status}`
            );
          }

          const problemResult = await problemResponse.json();
          const problemId = problemResult.problemId;

          for (const choice of choiceData) {
            const choiceFormData = new FormData();
            choiceFormData.append("Choices", choice.choices);
            choiceFormData.append("UnitOrder", choice.unitOrder);
            choiceFormData.append("ProblemId", problemId);
            if (choice.choiceImage) {
              choiceFormData.append("ChoiceImage", choice.choiceImage);
            }

            await fetch("https://localhost:7181/api/ProblemChoices", {
              method: "POST",
              body: choiceFormData,
            });
          }

          const selectedGroup = groups.find(
            (g) =>
              g.groupId === parseInt(document.getElementById("GroupId").value)
          );
          Problems.showSuccessMessage(problemResult.message);
          Problems.addProblemToTree({
            problemId: problemId,
            problemName: document.getElementById("ProblemName").value,
            groupName: selectedGroup ? selectedGroup.groupName : "N/A",
            mainDegree: parseInt(document.getElementById("MainDegree").value),
            problemHeader: document.getElementById("ProblemHeader").value,
            rightAnswer: parseInt(
              document.querySelector('input[name="RightAnswer"]:checked').value
            ),
            shuffle: document.getElementById("Shuffle").checked,
            groupId: parseInt(document.getElementById("GroupId").value),
            problemImagePath: problemImage
              ? `/Uploads/${problemImage.name}`
              : null,
          });
        } catch (err) {
          console.error("Error:", err);
          Problems.showErrorMessage(
            err.message || "Error adding problem. Please try again."
          );
        } finally {
          saveBtn.textContent = originalText;
          saveBtn.disabled = false;
          Problems.clearContent();
        }
      });
  },

  toggleImageUpload: function () {
    const imageUploadSection = document.getElementById("imageUploadSection");
    const imageCheck = document.getElementById("imageCheck");
    imageUploadSection.style.display = imageCheck.checked ? "block" : "none";
  },

  toggleChoiceImage: function (index) {
    const choiceImageSection = document.getElementById(
      `choiceImageSection_${index}`
    );
    const choiceImageCheck = document.getElementById(
      `choiceImageCheck_${index}`
    );
    choiceImageSection.style.display = choiceImageCheck.checked
      ? "block"
      : "none";
  },

  addProblemToTree: function (problem) {
    const problemsList = document.getElementById("problems-list");
    const li = document.createElement("li");
    li.className = "problem-item new-item";
    li.innerHTML = `
        <div class="material-content">
          <span class="material-name">${problem.problemName}</span>
          <span class="material-code">Group: ${
            problem.groupName || "N/A"
          }</span>
        </div>
      `;

    li.addEventListener("contextmenu", (e) => {
      e.preventDefault();
      e.stopPropagation();
      Problems.showProblemContextMenu(e, problem);
    });

    li.addEventListener("dblclick", (e) => {
      e.preventDefault();
      e.stopPropagation();
      Problems.showDetailsForm(problem);
    });

    li.addEventListener("click", (e) => {
      e.stopPropagation();
    });

    problemsList.appendChild(li);
    setTimeout(() => {
      li.classList.remove("new-item");
    }, 1000);
  },

  clearContent: function () {
    const content = document.getElementById("content");
    if (content) {
      content.innerHTML = `
        <div class="welcome-message">
          <h2>Welcome to Exams Management System</h2>
          <p>Select an item from the sidebar to begin</p>
        </div>
      `;
    }
  },

  showSuccessMessage: function (message) {
    const content = document.getElementById("content");
    if (content) {
      content.innerHTML = `
        <div class="content-display">
          <div class="message-container success">
            <div class="message-icon">✓</div>
            <h3>${message}</h3>
            <button onclick="Problems.clearContent()" class="btn-back">Return to Home</button>
          </div>
        </div>
      `;
    }
  },

  showErrorMessage: function (message) {
    const content = document.getElementById("content");
    if (content) {
      content.innerHTML = `
        <div class="content-display">
          <div class="message-container error">
            <div class="message-icon">✗</div>
            <h3>${message}</h3>
            <button onclick="Problems.clearContent()" class="btn-back">Return to Home</button>
          </div>
        </div>
      `;
    }
  },

  showErrorToast: function (message) {
    const toast = document.createElement("div");
    toast.className = "toast-custom";
    toast.style.position = "fixed";
    toast.style.bottom = "20px";
    toast.style.right = "20px";
    toast.style.zIndex = "1000";
    toast.style.backgroundColor = "#dc3545";
    toast.style.color = "white";
    toast.style.padding = "10px 20px";
    toast.style.borderRadius = "5px";
    toast.style.boxShadow = "0 2px 10px rgba(0,0,0,0.2)";

    toast.innerHTML = `
        <div style="display: flex; align-items: center;">
            <div style="flex-grow: 1;">${message}</div>
            <button onclick="this.parentElement.parentElement.remove()" style="background: none; border: none; color: white; cursor: pointer;">×</button>
        </div>
    `;

    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 5000);
  },
};

document.addEventListener("DOMContentLoaded", () => {
  Problems.initialize();
});
