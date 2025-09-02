const Groups = {
  initialize: function () {
    const groupsLabel = document.getElementById("groups-label");
    const groupsList = document.getElementById("groups-list");
    const toggleArrow = document.querySelector(".groups-toggle");

    groupsLabel.addEventListener("contextmenu", function (e) {
      e.preventDefault();
      Groups.showGroupsLabelContextMenu(e);
    });

    toggleArrow.addEventListener("click", (e) => {
      e.stopPropagation();
      Groups.toggleGroupsList();
    });

    groupsLabel.addEventListener("click", (e) => {
      e.stopPropagation();
      Groups.toggleGroupsList();
    });

    Groups.clearContent();
  },

  showGroupsLabelContextMenu: function (event) {
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let groupsLabelContextMenu = document.getElementById(
      "groupsLabelContextMenu"
    );

    if (!groupsLabelContextMenu) {
      groupsLabelContextMenu = document.createElement("ul");
      groupsLabelContextMenu.id = "groupsLabelContextMenu";
      groupsLabelContextMenu.className = "context-menu";
      groupsLabelContextMenu.style.position = "absolute";
      groupsLabelContextMenu.style.display = "none";
      groupsLabelContextMenu.style.zIndex = "1000";
      groupsLabelContextMenu.innerHTML = `<li id="groupAddOption">Add Group</li>`;
      document.body.appendChild(groupsLabelContextMenu);
    }

    groupsLabelContextMenu.style.left = `${event.pageX}px`;
    groupsLabelContextMenu.style.top = `${event.pageY}px`;
    groupsLabelContextMenu.style.display = "block";

    const newGroupsLabelContextMenu = groupsLabelContextMenu.cloneNode(true);
    groupsLabelContextMenu.parentNode.replaceChild(
      newGroupsLabelContextMenu,
      groupsLabelContextMenu
    );

    newGroupsLabelContextMenu
      .querySelector("#groupAddOption")
      .addEventListener("click", () => {
        Groups.showAddForm();
        newGroupsLabelContextMenu.style.display = "none";
      });

    document.addEventListener("click", function hideGroupsLabelContextMenu(e) {
      if (!newGroupsLabelContextMenu.contains(e.target)) {
        newGroupsLabelContextMenu.style.display = "none";
        document.removeEventListener("click", hideGroupsLabelContextMenu);
      }
    });
  },

  toggleGroupsList: function () {
    const groupsList = document.getElementById("groups-list");
    const toggleArrow = document.querySelector(".groups-toggle");

    if (
      groupsList.style.display === "none" ||
      groupsList.style.display === ""
    ) {
      Groups.fetchGroups();
      groupsList.style.display = "block";
      toggleArrow.innerHTML = "▲";
      toggleArrow.classList.add("expanded");
    } else {
      groupsList.style.display = "none";
      toggleArrow.innerHTML = "▼";
      toggleArrow.classList.remove("expanded");
    }
  },

  fetchGroups: function () {
    const groupsList = document.getElementById("groups-list");
    groupsList.innerHTML =
      '<li class="loading-item"><i class="loading-spinner"></i> Loading...</li>';

    fetch("https://localhost:7181/api/Groups")
      .then((response) => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
      })
      .then((groups) => {
        groupsList.innerHTML = "";
        if (groups && groups.length > 0) {
          groups.forEach((group, index) => {
            const li = document.createElement("li");
            li.className = "group-item";
            li.innerHTML = `
                <div class="material-content">
                  <span class="material-name">${group.groupName}</span>
                  <span class="material-code">Topic: ${
                    group.topicName || "N/A"
                  }</span>
                </div>
              `;
            li.style.animationDelay = `${index * 0.1}s`;

            li.addEventListener("contextmenu", (e) => {
              e.preventDefault();
              e.stopPropagation();
              Groups.showGroupContextMenu(e, group);
            });

            li.addEventListener("dblclick", (e) => {
              e.preventDefault();
              e.stopPropagation();
              Groups.showDetailsForm(group);
            });

            li.addEventListener("click", (e) => {
              e.stopPropagation();
            });

            groupsList.appendChild(li);
          });
        } else {
          groupsList.innerHTML =
            '<li class="no-materials">No groups available</li>';
        }
      })
      .catch((err) => {
        console.error("Error fetching groups:", err);
        groupsList.innerHTML = `<li class="error-item">Error loading groups: ${err.message}</li>`;
      });
  },

  fetchTopics: async function () {
    try {
      const response = await fetch("https://localhost:7181/api/Groups/topics");
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      return await response.json();
    } catch (err) {
      console.error("Error fetching topics:", err);
      return [];
    }
  },

  showGroupContextMenu: function (event, group) {
    const allContextMenus = document.querySelectorAll(".context-menu");
    allContextMenus.forEach((menu) => (menu.style.display = "none"));

    let groupContextMenu = document.getElementById("groupContextMenu");

    if (!groupContextMenu) {
      groupContextMenu = document.createElement("ul");
      groupContextMenu.id = "groupContextMenu";
      groupContextMenu.className = "context-menu";
      groupContextMenu.style.position = "absolute";
      groupContextMenu.style.display = "none";
      groupContextMenu.style.zIndex = "1000";
      groupContextMenu.innerHTML = `
          <li id="groupDetailsOption">View Details</li>
          <li id="groupEditOption">Edit</li>
          <li id="groupDeleteOption">Delete</li>
        `;
      document.body.appendChild(groupContextMenu);
    }

    groupContextMenu.style.left = `${event.pageX}px`;
    groupContextMenu.style.top = `${event.pageY}px`;
    groupContextMenu.style.display = "block";

    const newGroupContextMenu = groupContextMenu.cloneNode(true);
    groupContextMenu.parentNode.replaceChild(
      newGroupContextMenu,
      groupContextMenu
    );

    newGroupContextMenu
      .querySelector("#groupDetailsOption")
      .addEventListener("click", () => {
        Groups.showDetailsForm(group);
        newGroupContextMenu.style.display = "none";
      });

    newGroupContextMenu
      .querySelector("#groupEditOption")
      .addEventListener("click", () => {
        Groups.showEditForm(group);
        newGroupContextMenu.style.display = "none";
      });

    newGroupContextMenu
      .querySelector("#groupDeleteOption")
      .addEventListener("click", () => {
        Groups.showDeleteForm(group);
        newGroupContextMenu.style.display = "none";
      });

    document.addEventListener("click", function hideGroupContextMenu(e) {
      if (!newGroupContextMenu.contains(e.target)) {
        newGroupContextMenu.style.display = "none";
        document.removeEventListener("click", hideGroupContextMenu);
      }
    });
  },

  showDetailsForm: function (group) {
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Group Details</h3>
          <div class="material-details">
            <div class="detail-item">
              <span class="detail-label">Group Name:</span>
              <span class="detail-value">${group.groupName}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Topic:</span>
              <span class="detail-value">${group.topicName || "N/A"}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Total Problems:</span>
              <span class="detail-value">${group.totalProblems}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Main Degree:</span>
              <span class="detail-value">${group.mainDegree}</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Has Common Header:</span>
              <span class="detail-value">${
                group.hasCommonHeader ? "Yes" : "No"
              }</span>
            </div>
            <div class="detail-item">
              <span class="detail-label">Common Question Header:</span>
              <span class="detail-value">${
                group.commonQuestionHeader || "N/A"
              }</span>
            </div>
          </div>
          <div class="button-group">
            <button type="button" class="btn-cancel" onclick="Groups.clearContent()">Close</button>
          </div>
        </div>
      `;
  },

  showEditForm: async function (group) {
    const topics = await Groups.fetchTopics();
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Edit Group</h3>
          <form id="editGroupForm" class="material-form">
            <div class="input-item">
              <label for="GroupName">Group Name</label>
              <input type="text" id="GroupName" value="${
                group.groupName
              }" required maxlength="40" />
            </div>
            <div class="input-item">
              <label for="MainDegree">Main Degree</label>
              <input type="text" id="MainDegree" value="${
                group.mainDegree
              }" required pattern="\\d+" title="Please enter a positive number" />
            </div>
            <div class="input-item">
              <label for="TotalProblems">Total Problems</label>
              <input type="text" id="TotalProblems" value="${
                group.totalProblems
              }" required pattern="\\d+" title="Please enter a positive number" />
            </div>
            <div class="input-item">
              <label for="TopicId">Topic</label>
              <select id="TopicId" required>
                <option value="">Select a topic</option>
                ${topics
                  .map(
                    (topic) =>
                      `<option value="${topic.topicId}" ${
                        topic.topicId === group.topicId ? "selected" : ""
                      }>${topic.topicName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="input-item checkbox">
              <label for="HasCommonHeader">Has Common Header</label>
              <input type="checkbox" id="HasCommonHeader" ${
                group.hasCommonHeader ? "checked" : ""
              } />
            </div>
            <div class="input-item" id="commonHeaderSection" style="${
              group.hasCommonHeader ? "" : "display: none;"
            }">
              <label for="CommonQuestionHeader">Common Question Header</label>
              <textarea id="CommonQuestionHeader" maxlength="1000">${
                group.commonQuestionHeader || ""
              }</textarea>
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Changes</button>
              <button type="button" class="btn-cancel" onclick="Groups.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;

    const checkbox = document.getElementById("HasCommonHeader");
    const commonHeaderSection = document.getElementById("commonHeaderSection");
    checkbox.addEventListener("change", function () {
      commonHeaderSection.style.display = this.checked ? "block" : "none";
    });

    document
      .getElementById("editGroupForm")
      .addEventListener("submit", function (e) {
        e.preventDefault();

        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
        saveBtn.disabled = true;

        const data = {
          groupName: document.getElementById("GroupName").value,
          mainDegree: parseInt(document.getElementById("MainDegree").value),
          totalProblems: parseInt(
            document.getElementById("TotalProblems").value
          ),
          topicId: parseInt(document.getElementById("TopicId").value),
          commonQuestionHeader: document.getElementById("CommonQuestionHeader")
            .value,
          hasCommonHeader: document.getElementById("HasCommonHeader").checked,
        };

        fetch(`https://localhost:7181/api/Groups/${group.groupId}`, {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(data),
        })
          .then((res) => {
            if (!res.ok) {
              throw new Error(`HTTP error! status: ${res.status}`);
            }
            return res.json();
          })
          .then((response) => {
            Groups.showSuccessMessage(response.message);
            Groups.fetchGroups();
            Groups.clearContent();
          })
          .catch((err) => {
            console.error("Error:", err);
            Groups.showErrorMessage("Error updating group. Please try again.");
          })
          .finally(() => {
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
          });
      });
  },

  showDeleteForm: function (group) {
    const content = document.getElementById("content");
    content.innerHTML = `
      <div class="content-display">
        <h3 class="content-title">Delete Group</h3>
        <p class="delete-message">
          Are you sure you want to delete the group 
          <strong>${group.groupName}</strong>?
        </p>
        <div class="button-group">
          <button type="button" class="btn-delete" onclick="Groups.deleteGroup(${group.groupId})">Delete</button>
          <button type="button" class="btn-cancel" onclick="Groups.clearContent()">Cancel</button>
        </div>
      </div>
    `;
  },

  deleteGroup: function (groupId) {
    const deleteBtn = document.querySelector(".btn-delete");
    const originalText = deleteBtn.textContent;
    deleteBtn.innerHTML = '<i class="loading-spinner"></i> Deleting...';
    deleteBtn.disabled = true;

    fetch(`https://localhost:7181/api/Groups/${groupId}`, {
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
        Groups.showSuccessMessage(response.message);
        Groups.fetchGroups();
        Groups.clearContent();
      })
      .catch((err) => {
        console.error("Error:", err);
        Groups.showErrorMessage("Error deleting group. Please try again.");
      })
      .finally(() => {
        deleteBtn.textContent = originalText;
        deleteBtn.disabled = false;
      });
  },

  showAddForm: async function () {
    const topics = await Groups.fetchTopics();
    const content = document.getElementById("content");
    content.innerHTML = `
        <div class="content-display">
          <h3 class="content-title">Add New Group</h3>
          <form id="groupForm" class="material-form">
            <div class="input-item">
              <label for="GroupName">Group Name</label>
              <input type="text" id="GroupName" placeholder="Enter group name" required maxlength="40" />
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
              <label for="TopicId">Topic</label>
              <select id="TopicId" required>
                <option value="">Select a topic</option>
                ${topics
                  .map(
                    (topic) =>
                      `<option value="${topic.topicId}">${topic.topicName}</option>`
                  )
                  .join("")}
              </select>
            </div>
            <div class="input-item checkbox">
              <label for="HasCommonHeader">Has Common Header</label>
              <input type="checkbox" id="HasCommonHeader" />
            </div>
            <div class="input-item" id="commonHeaderSection" style="display: none;">
              <label for="CommonQuestionHeader">Common Question Header</label>
              <textarea id="CommonQuestionHeader" placeholder="Enter common question header" maxlength="1000"></textarea>
            </div>
            <div class="button-group">
              <button type="submit" class="btn-save">Save Group</button>
              <button type="button" class="btn-cancel" onclick="Groups.clearContent()">Cancel</button>
            </div>
          </form>
        </div>
      `;

    const checkbox = document.getElementById("HasCommonHeader");
    const commonHeaderSection = document.getElementById("commonHeaderSection");
    checkbox.addEventListener("change", function () {
      commonHeaderSection.style.display = this.checked ? "block" : "none";
    });

    document
      .getElementById("groupForm")
      .addEventListener("submit", function (e) {
        e.preventDefault();

        const saveBtn = document.querySelector(".btn-save");
        const originalText = saveBtn.textContent;
        saveBtn.innerHTML = '<i class="loading-spinner"></i> Saving...';
        saveBtn.disabled = true;

        const data = {
          groupName: document.getElementById("GroupName").value,
          mainDegree: parseInt(document.getElementById("MainDegree").value),
          totalProblems: parseInt(
            document.getElementById("TotalProblems").value
          ),
          topicId: parseInt(document.getElementById("TopicId").value),
          commonQuestionHeader: document.getElementById("CommonQuestionHeader")
            .value,
          hasCommonHeader: document.getElementById("HasCommonHeader").checked,
        };

        fetch("https://localhost:7181/api/Groups", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify(data),
        })
          .then((res) => {
            if (!res.ok) {
              return res.json().then((errorData) => {
                throw new Error(
                  errorData.message || `HTTP error! status: ${res.status}`
                );
              });
            }
            return res.json();
          })
          .then(async (response) => {
            const selectedTopic = topics.find(
              (t) => t.topicId === data.topicId
            );
            const group = {
              groupId: response.groupId,
              groupName: data.groupName,
              topicName: selectedTopic ? selectedTopic.topicName : "N/A",
              mainDegree: data.mainDegree,
              totalProblems: data.totalProblems,
              topicId: data.topicId,
              commonQuestionHeader: data.commonQuestionHeader,
              hasCommonHeader: data.hasCommonHeader,
            };
            Groups.showSuccessMessage(response.message);
            Groups.addGroupToTree(group);
            Groups.clearContent();
          })
          .catch((err) => {
            console.error("Error:", err);
            Groups.showErrorMessage(
              err.message || "Error adding group. Please try again."
            );
          })
          .finally(() => {
            saveBtn.textContent = originalText;
            saveBtn.disabled = false;
          });
      });
  },

  addGroupToTree: function (group) {
    const groupsList = document.getElementById("groups-list");
    const li = document.createElement("li");
    li.className = "group-item new-item";
    li.innerHTML = `
        <div class="material-content">
          <span class="material-name">${group.groupName}</span>
          <span class="material-code">Topic: ${group.topicName || "N/A"}</span>
        </div>
      `;

    li.addEventListener("contextmenu", (e) => {
      e.preventDefault();
      e.stopPropagation();
      Groups.showGroupContextMenu(e, group);
    });

    li.addEventListener("dblclick", (e) => {
      e.preventDefault();
      e.stopPropagation();
      Groups.showDetailsForm(group);
    });

    li.addEventListener("click", (e) => {
      e.stopPropagation();
    });

    groupsList.appendChild(li);
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
            <button onclick="Groups.clearContent()" class="btn-back">Return to Home</button>
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
            <button onclick="Groups.clearContent()" class="btn-back">Return to Home</button>
          </div>
        </div>
      `;
  },
};

document.addEventListener("DOMContentLoaded", Groups.initialize);
