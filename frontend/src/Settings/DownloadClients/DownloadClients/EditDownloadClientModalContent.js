import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import Card from 'Components/Card';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import ProviderFieldFormGroup from 'Components/Form/ProviderFieldFormGroup';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { icons, inputTypes, kinds } from 'Helpers/Props';
import AdvancedSettingsButton from 'Settings/AdvancedSettingsButton';
import translate from 'Utilities/String/translate';
import AddCategoryModalConnector from './Categories/AddCategoryModalConnector';
import Category from './Categories/Category';
import styles from './EditDownloadClientModalContent.css';

class EditDownloadClientModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isAddCategoryModalOpen: false
    };
  }

  onAddCategoryPress = () => {
    this.setState({ isAddCategoryModalOpen: true });
  };

  onAddCategoryModalClose = () => {
    this.setState({ isAddCategoryModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      advancedSettings,
      isFetching,
      error,
      categories,
      isSaving,
      isTesting,
      saveError,
      item,
      onInputChange,
      onFieldChange,
      onModalClose,
      onSavePress,
      onTestPress,
      onAdvancedSettingsPress,
      onDeleteDownloadClientPress,
      onConfirmDeleteCategory,
      ...otherProps
    } = this.props;

    const {
      isAddCategoryModalOpen
    } = this.state;

    const {
      id,
      implementationName,
      name,
      enable,
      priority,
      supportsCategories,
      fields,
      message
    } = item;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {id ? translate('EditDownloadClientImplementation', { implementationName }) : translate('AddDownloadClientImplementation', { implementationName })}
        </ModalHeader>

        <ModalBody>
          {
            isFetching &&
              <LoadingIndicator />
          }

          {
            !isFetching && !!error &&
              <div>
                {translate('UnableToAddANewDownloadClientPleaseTryAgain')}
              </div>
          }

          {
            !isFetching && !error &&
              <Form {...otherProps}>
                {
                  !!message &&
                    <Alert
                      className={styles.message}
                      kind={message.value.type}
                    >
                      {message.value.message}
                    </Alert>
                }

                <FormGroup>
                  <FormLabel>{translate('Name')}</FormLabel>

                  <FormInputGroup
                    type={inputTypes.TEXT}
                    name="name"
                    {...name}
                    onChange={onInputChange}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel>{translate('Enable')}</FormLabel>

                  <FormInputGroup
                    type={inputTypes.CHECK}
                    name="enable"
                    {...enable}
                    onChange={onInputChange}
                  />
                </FormGroup>

                {
                  fields.map((field) => {
                    return (
                      <ProviderFieldFormGroup
                        key={field.name}
                        advancedSettings={advancedSettings}
                        provider="downloadClient"
                        providerData={item}
                        {...field}
                        onChange={onFieldChange}
                      />
                    );
                  })
                }

                <FormGroup
                  advancedSettings={advancedSettings}
                  isAdvanced={true}
                >
                  <FormLabel>{translate('ClientPriority')}</FormLabel>

                  <FormInputGroup
                    type={inputTypes.NUMBER}
                    name="priority"
                    helpText={translate('DownloadClientPriorityHelpText')}
                    min={1}
                    max={50}
                    {...priority}
                    onChange={onInputChange}
                  />
                </FormGroup>

                {
                  supportsCategories.value ?
                    <FieldSet legend={translate('MappedCategories')}>
                      <div className={styles.customFormats}>
                        {
                          categories.map((tag) => {
                            return (
                              <Category
                                key={tag.id}
                                {...tag}
                                onConfirmDeleteSpecification={onConfirmDeleteCategory}
                              />
                            );
                          })
                        }

                        <Card
                          className={styles.addCategory}
                          onPress={this.onAddCategoryPress}
                        >
                          <div className={styles.center}>
                            <Icon
                              name={icons.ADD}
                              size={25}
                            />
                          </div>
                        </Card>
                      </div>
                    </FieldSet> :
                    null
                }

                <AddCategoryModalConnector
                  isOpen={isAddCategoryModalOpen}
                  onModalClose={this.onAddCategoryModalClose}
                />

              </Form>
          }
        </ModalBody>
        <ModalFooter>
          {
            id &&
              <Button
                className={styles.deleteButton}
                kind={kinds.DANGER}
                onPress={onDeleteDownloadClientPress}
              >
                {translate('Delete')}
              </Button>
          }

          <AdvancedSettingsButton
            advancedSettings={advancedSettings}
            onAdvancedSettingsPress={onAdvancedSettingsPress}
            showLabel={false}
          />

          <SpinnerErrorButton
            isSpinning={isTesting}
            error={saveError}
            onPress={onTestPress}
          >
            {translate('Test')}
          </SpinnerErrorButton>

          <Button
            onPress={onModalClose}
          >
            {translate('Cancel')}
          </Button>

          <SpinnerErrorButton
            isSpinning={isSaving}
            error={saveError}
            onPress={onSavePress}
          >
            {translate('Save')}
          </SpinnerErrorButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

EditDownloadClientModalContent.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isTesting: PropTypes.bool.isRequired,
  categories: PropTypes.arrayOf(PropTypes.object),
  item: PropTypes.object.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onFieldChange: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onTestPress: PropTypes.func.isRequired,
  onAdvancedSettingsPress: PropTypes.func.isRequired,
  onDeleteDownloadClientPress: PropTypes.func,
  onConfirmDeleteCategory: PropTypes.func.isRequired
};

export default EditDownloadClientModalContent;
