﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ardoq;
using Ardoq.Models;
using Ardoq.Service;
using Ardoq.Service.Interface;
using ArdoqTest.Helper;
using NUnit.Framework;

namespace ArdoqTest.Service
{
    [TestFixture]
    public class WorkspaceServiceTest
    {
        private IWorkspaceService service;
        private IArdoqClient client;


        [TestFixtureSetUp]
        public void Before()
        {
            client = TestUtils.GetClient();
            service = client.WorkspaceService;
        }

        private static Workspace CreateWorkspaceTemplate()
        {
            return new Workspace("Test Workspace", TestUtils.GetTestPropery("modelId"), "Hello world!");
        }

        [Test]
        public async void BranchWorkspaceTest()
        {
            Workspace workspaceTemplate = CreateWorkspaceTemplate();
            Workspace result = await service.CreateWorkspace(workspaceTemplate);
            Workspace myBranch = await service.BranchWorkspace(result.Id, new WorkspaceBranchRequest("myBranch"));

            Assert.True(result.Id == myBranch.Origin.Id);
            Assert.True("myBranch" == myBranch.Name);

            Assert.True(result.VersionCounter == myBranch.Origin.VersionCounter);
            List<WorkspaceBranch> branches = await service.GetBranches(result.Id);

            Assert.True(1 == branches.Count);
            Assert.True(myBranch.Name == branches[0].BranchName);

            // rolling back
            await service.DeleteWorkspace(result.Id);
        }


        [Test]
        public async void CreateWorkspaceTest()
        {
            Workspace workspaceTemplate = CreateWorkspaceTemplate();
            Workspace result = await service.CreateWorkspace(workspaceTemplate);
            Assert.NotNull(result.Id);
            // roll back
            await service.DeleteWorkspace(result.Id);
        }

        [Test]
        public async void DeleteWorkspaceTest()
        {
            Workspace workspaceTemplate = CreateWorkspaceTemplate();
            Workspace result = await service.CreateWorkspace(workspaceTemplate);
            try
            {
                await service.DeleteWorkspace(result.Id);
            }
            catch (Exception)
            {
                Assert.Fail("Expected the workspace to be existing before deletion.");
            }

            try
            {
                await service.GetWorkspaceById(result.Id);
                Assert.Fail("Expected the workspace to be deleted.");
            }
            catch (Exception e)
            {
                Assert.NotNull(e);
            }
        }

        [Test]
        public async void GetAggregatedWorkspaceTest()
        {
            var workspace = new Workspace("Test Workspace", TestUtils.GetTestPropery("modelId"), "Hello world!");

            Workspace aggregatedWorkspace = await service.CreateWorkspace(workspace);
            Workspace targetWorkspace = await service.CreateWorkspace(workspace);
            Workspace sourceWorkspace = await service.CreateWorkspace(workspace);

            await client.ComponentService.CreateComponent(new Component("Test Component", aggregatedWorkspace.Id,
                "Test Component"));

            await client.ReferenceService.CreateReference(new Reference(aggregatedWorkspace.Id, "test reference",
                sourceWorkspace.Id, targetWorkspace.Id, 0));

            await client.TagService.CreateTag(new Tag("Test Tag", aggregatedWorkspace.Id, "Test tag"));

            AggregatedWorkspace result = await service.GetAggregatedWorkspace(aggregatedWorkspace.Id);
            Assert.True(result.Components.Any());
            Assert.True(result.References.Any());
            Assert.True(result.Tags.Any());

            // rolling back
            await service.DeleteWorkspace(aggregatedWorkspace.Id);
            await service.DeleteWorkspace(targetWorkspace.Id);
            await service.DeleteWorkspace(sourceWorkspace.Id);
        }

        [Test]
        public async void GetWorkspaceTest()
        {
            Workspace workspaceTemplate = CreateWorkspaceTemplate();
            Workspace workspace = await service.CreateWorkspace(workspaceTemplate);
            Workspace result = await service.GetWorkspaceById(workspace.Id);
            Assert.True(workspace.Id == result.Id);

            // roll back
            await service.DeleteWorkspace(workspace.Id);
        }

        [Test]
        public async void UpdateWorkspaceTest()
        {
            Workspace workspaceTemplate = CreateWorkspaceTemplate();
            Workspace result = await service.CreateWorkspace(workspaceTemplate);
            result.Name = "updatedName";
            Workspace updatedWorkspace = await service.UpdateWorkspace(result.Id, result);
            Assert.True("updatedName" == updatedWorkspace.Name);
            //roll back
            await service.DeleteWorkspace(result.Id);
        }
    }
}